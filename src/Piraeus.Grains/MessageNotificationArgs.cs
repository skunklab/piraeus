﻿using System;
using Piraeus.Core.Messaging;

namespace Piraeus.Grains
{
    [Serializable]
    public class MessageNotificationArgs
    {
        public MessageNotificationArgs(EventMessage message)
        {
            Message = message;
            Timestamp = DateTime.UtcNow;
        }

        public EventMessage Message
        {
            get; internal set;
        }

        public DateTime? Timestamp
        {
            get; internal set;
        }
    }
}