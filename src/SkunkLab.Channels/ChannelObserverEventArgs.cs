﻿using System;

namespace SkunkLab.Channels
{
    public class ChannelObserverEventArgs : EventArgs
    {
        public ChannelObserverEventArgs(string channelId, string resourceUriString, string contentType, byte[] message)
        {
            ChannelId = channelId;
            ResourceUriString = resourceUriString;
            ContentType = contentType;
            Message = message;
        }

        public string ChannelId
        {
            get; internal set;
        }

        public string ContentType
        {
            get; internal set;
        }

        public byte[] Message
        {
            get; internal set;
        }

        public string ResourceUriString
        {
            get; internal set;
        }
    }
}