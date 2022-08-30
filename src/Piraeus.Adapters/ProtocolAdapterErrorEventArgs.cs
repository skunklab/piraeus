﻿using System;

namespace Piraeus.Adapters
{
    public class ProtocolAdapterErrorEventArgs : EventArgs
    {
        public ProtocolAdapterErrorEventArgs(string channelId, Exception error)
        {
            ChannelId = channelId;
            Error = error;
        }

        public string ChannelId
        {
            get; internal set;
        }

        public Exception Error
        {
            get; internal set;
        }
    }
}