﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace SkunkLab.Protocols.Mqtt
{
    public delegate void ExpiredExchangeEventHandler(object sender, LifetimeEventArgs args);

    public class ExchangeLifetime
    {
        private readonly Dictionary<ushort, DateTime> container;

        private readonly TimeSpan lifetime;

        private readonly Timer timer;

        public ExchangeLifetime(TimeSpan interval, TimeSpan lifetime)
        {
            this.lifetime = lifetime;
            container = new Dictionary<ushort, DateTime>();
            timer = new Timer(interval.TotalMilliseconds);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public event ExpiredExchangeEventHandler OnExpired;

        public void Add(ushort id)
        {
            if (!container.ContainsKey(id))
            {
                container.Add(id, DateTime.UtcNow.AddMilliseconds(lifetime.TotalMilliseconds));
            }

            if (!timer.Enabled)
            {
                timer.Enabled = true;
            }
        }

        public bool IsProcessed(ushort id)
        {
            return container.ContainsKey(id);
        }

        public void Remove(ushort id)
        {
            container.Remove(id);

            if (container.Count == 0)
            {
                timer.Enabled = false;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<ushort> list = new List<ushort>();
            DateTime now = DateTime.UtcNow;

            var query = container.Where(c => c.Value < now);

            if (query != null)
            {
                foreach (var item in query)
                {
                    if (container.ContainsKey(item.Key))
                    {
                        list.Add(item.Key);
                    }
                }

                ushort[] ids = list.ToArray();

                foreach (var item in list)
                    container.Remove(item);

                if (ids != null && ids.Length > 0)
                {
                    OnExpired?.Invoke(this, new LifetimeEventArgs(ids));
                }
            }

            if (container.Count == 0)
            {
                timer.Enabled = false;
            }
        }
    }
}