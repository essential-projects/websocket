
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using EssentialProjects.WebSocket;
using EssentialProjects.WebSocket.Contracts;
using Newtonsoft.Json.Linq;

namespace test_lib_.NET_client
{
    public class ExampleMessageType
    {
        public string TestMessage { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Test().Wait();
        }

        static async Task Test()
        {
            while (true)
            {
                using (var socket = new ClientWebSocket())
                {
                    socket.ConnectAsync(new Uri("ws://localhost:8001"), CancellationToken.None)
                      .Wait(CancellationToken.None);
                    var socketClient = new SocketClient(socket);

                    socketClient.On<ExampleMessageType>("my_test", (ExampleMessageType message) =>
                    {
                        Console.WriteLine(message.TestMessage);
                    });

                    await socketClient.StartListening(CancellationToken.None);
                }
            }
        }
    }
}
