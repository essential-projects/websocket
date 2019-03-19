
namespace EssentialProjects.WebSocket
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using EssentialProjects.WebSocket.Contracts;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class SocketClient : ISocketClient
    {
        private ClientWebSocket Socket;
        private string ChannelName;
        private Dictionary<string, EventTypeConfiguration> EventListeners;

        /// <summary>
        /// Creates a new socket client.
        /// </summary>
        /// <param name="socket">
        /// The socket used to communicate with the endpoint.
        /// </param>
        /// <param name="channelName">
        /// The name of the channel the socket client uses to send and receive
        /// messages.
        /// </param>
        public SocketClient(ClientWebSocket socket, string channelName = null)
        {
            this.Socket = socket;
            this.ChannelName = channelName;
            this.EventListeners = new Dictionary<string, EventTypeConfiguration>();
        }

        public void RegisterMessageType<TMessageType>(string eventType)
        {
            var eventTypeConfiguration = new EventTypeConfiguration(eventType, typeof(TMessageType));
            this.EventListeners[eventType] = eventTypeConfiguration;
        }

        public async Task StartListening(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    var message = await this.ReceiveFullMessage(this.Socket, cancellationToken);
                    this.HandleMessage(message.Item1, message.Item2);
                }
            }
            catch (WebSocketException ex)
            {
                switch (ex.WebSocketErrorCode)
                {
                    case WebSocketError.ConnectionClosedPrematurely:
                        break;
                    default:
                        break;
                }
                throw;
            }
        }

        public async Task ReceiveMessages(WebSocket socket, CancellationToken cancelToken)
        {
            const int maxMessageSize = 4096;
            var buffer = new byte[maxMessageSize];

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var (response, message) = await ReceiveFullMessage(socket, cancelToken);
                    if (response.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    HandleMessage(response, message);
                }
                catch (OperationCanceledException)
                {
                    // Exit normally
                }
            }
        }

        public async Task<(WebSocketReceiveResult, IEnumerable<byte>)> ReceiveFullMessage(WebSocket socket, CancellationToken cancellationToken)
        {
            WebSocketReceiveResult response;
            var message = new List<byte>();

            var buffer = new byte[4096];
            do
            {
                if (socket.State != WebSocketState.Open)
                {
                    return (null, null);
                }
                response = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                message.AddRange(new ArraySegment<byte>(buffer, 0, response.Count));
            } while (!response.EndOfMessage);

            return (response, message);
        }

        public Subscription On<TMessage>(string eventType, Action<TMessage> callback) where TMessage : class
        {
            var keyNotFound = !this.EventListeners.ContainsKey(eventType);
            if (keyNotFound)
            {
                this.EventListeners[eventType] = new EventTypeConfiguration(eventType, typeof(TMessage));
            }

            var eventTypeConfiguration = this.EventListeners[eventType];

            var listenerId = Guid.NewGuid();
            var listener = new Listener<TMessage>(listenerId, eventType, typeof(TMessage), callback, this.DisposeListener);

            eventTypeConfiguration.EventListeners.Add(listener);

            return new Subscription(listener.Dispose);
        }

        private void DisposeListener(IListener listener)
        {
            var eventTypeConfiguration = this.EventListeners[listener.EventType];

            eventTypeConfiguration.EventListeners.Remove(listener);
        }

        public Subscription Once<TMessage>(string eventType, Action<TMessage> callback) where TMessage : class
        {
            Subscription subscription = null;

            Action<TMessage> innerCallback = (message) => {
                callback(message);
                if (subscription != null) {
                    subscription.Dispose();
                }
            };

            subscription = this.On(eventType, innerCallback);

            return subscription;
        }

        public void Dispose()
        {
            this.EventListeners = new Dictionary<string, EventTypeConfiguration>();
        }

        private TFinalType ConvertMessage<TFinalType>(TFinalType targetType, object data) {
            return (TFinalType)data;
        }

        private void HandleMessage(WebSocketReceiveResult response, IEnumerable<byte> rawMessage)
        {
            if (rawMessage == null)
            {
                return;
            }

            var jsonResponse = Encoding.UTF8.GetString(rawMessage.ToArray());

            MessageEnvelope<object> messageEnvelope = (MessageEnvelope<object>)JsonConvert.DeserializeObject(jsonResponse, typeof(MessageEnvelope<object>));

            if (messageEnvelope == null)
            {
                return;
            }

            var hasEndpointName = !String.IsNullOrEmpty(this.ChannelName);
            var isNotCorrectEndpoint = this.ChannelName != messageEnvelope.ChannelName;

            if (hasEndpointName && isNotCorrectEndpoint)
            {
                return;
            }

            var hasNoEventTypeDefinition = !this.EventListeners.ContainsKey(messageEnvelope.EventType);

            if (hasNoEventTypeDefinition)
            {
                return;
            }

            var eventTypeConfiguration = this.EventListeners[messageEnvelope.EventType];

            if (eventTypeConfiguration.HasNoEventListeners())
            {
                return;
            }

            Console.WriteLine("Received message on channel \"{0}\" for event type \"{1}\"", messageEnvelope.ChannelName, messageEnvelope.EventType);

            var messageContent = messageEnvelope.Message.ToString();
            var deserializedMessage = JsonConvert.DeserializeObject(messageContent, eventTypeConfiguration.MessageType);

            var eventListeners = eventTypeConfiguration.EventListeners.ToArray();
            foreach (var eventListener in eventListeners)
            {
                eventListener.ExecuteCallback(deserializedMessage);
            }
        }
    }
}
