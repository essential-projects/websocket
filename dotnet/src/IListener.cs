namespace EssentialProjects.WebSocket
{
  using System;

  using Newtonsoft.Json.Linq;

  internal interface IListener : IDisposable
  {
    Type MessageType { get; }
    string EventType { get; }
    Guid Id { get; }
    void ExecuteCallback(object message);
  }
}
