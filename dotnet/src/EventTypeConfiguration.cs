namespace EssentialProjects.WebSocket
{
    using System;
    using System.Collections.Generic;

    internal class EventTypeConfiguration
    {
        public List<IListener> EventListeners;
        public string EventType;
        public Type MessageType;

        public EventTypeConfiguration(string eventType, Type messageType)
        {
            this.EventListeners = new List<IListener>();
            this.EventType = eventType;
            this.MessageType = messageType;
        }

        public bool HasNoEventListeners()
        {
            return this.EventListeners == null | this.EventListeners.Count == 0;
        }
    }
}
