﻿using System;

namespace SkunkLab.Channels.Http
{
    public class HttpObserver : Observer
    {
        public HttpObserver(Uri resourceUri)
        {
            ResourceUri = resourceUri;
        }

        public override event ObserverEventHandler OnNotify;

        public override Uri ResourceUri
        {
            get; set;
        }

        public override void Update(Uri resourceUri, string contentType, byte[] message)
        {
            OnNotify?.Invoke(this, new ObserverEventArgs(resourceUri, contentType, message));
        }
    }
}