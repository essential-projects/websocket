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

    internal class Listener<TMessage> : IListener where TMessage : class
    {
        private Action<TMessage> Callback;
        private Action<IListener> DisposeCallback;
        public Guid Id { get; private set; }
        public string EventType { get; private set; }
        public Type MessageType { get; private set; }

        public Listener(Guid listenerId, Type messageType, Action<TMessage> callback, Action<IListener> disposeCallback)
        {
            this.Id = listenerId;
            this.MessageType = messageType;
            this.Callback = callback;
            this.DisposeCallback = disposeCallback;
        }

        public void Dispose()
        {
            this.DisposeCallback(this);
        }

        public void ExecuteCallback(object message)
        {
            this.Callback(message as TMessage);
        }
    }

}
