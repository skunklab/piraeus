﻿using System;

namespace SkunkLab.Channels
{
    public delegate void ObserverEventHandler(object sender, ObserverEventArgs args);

    public abstract class Observer
    {
        public abstract event ObserverEventHandler OnNotify;

        public abstract Uri ResourceUri
        {
            get; set;
        }

        public abstract void Update(Uri resourceUri, string contentType, byte[] message);
    }
}