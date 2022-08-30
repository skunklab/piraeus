﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace SkunkLab.Protocols.Coap
{
    public class Transmitter : IDisposable
    {
        private readonly Dictionary<ushort, Tuple<string, DateTime, Action<CodeType, string, byte[]>>> container;

        private readonly double lifetimeMilliseconds;

        private readonly int maxAttempts;

        private readonly Dictionary<string, Action<CodeType, string, byte[]>> observeContainer;

        private readonly Dictionary<ushort, Tuple<DateTime, int, CoapMessage>> retryContainer;

        private readonly double retryMilliseconds;

        private readonly Timer timer;

        private ushort currentId;

        private bool disposedValue;

        public Transmitter(double lifetimeMilliseconds, double retryMilliseconds, int maxRetryAttempts)
        {
            this.lifetimeMilliseconds = lifetimeMilliseconds;
            this.retryMilliseconds = retryMilliseconds;
            maxAttempts = maxRetryAttempts;
            timer = new Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            retryContainer = new Dictionary<ushort, Tuple<DateTime, int, CoapMessage>>();
            container = new Dictionary<ushort, Tuple<string, DateTime, Action<CodeType, string, byte[]>>>();
            observeContainer = new Dictionary<string, Action<CodeType, string, byte[]>>();
        }

        public event EventHandler<CoapMessageEventArgs> OnRetry;

        public void AddMessage(CoapMessage message)
        {
            if (message.MessageType == CoapMessageType.Confirmable)
            {
                if (!retryContainer.ContainsKey(message.MessageId))
                {
                    retryContainer.Add(message.MessageId,
                        new Tuple<DateTime, int, CoapMessage>(DateTime.UtcNow.AddMilliseconds(retryMilliseconds), 0,
                            message));
                }
            }
        }

        public void Clear()
        {
            container.Clear();
            retryContainer.Clear();
            timer.Enabled = false;
        }

        public void DispatchResponse(CoapMessage message)
        {
            var observeQuery = observeContainer.Where(c => c.Key == Convert.ToBase64String(message.Token));

            foreach (var item in observeQuery)
                item.Value(message.Code, MediaTypeConverter.ConvertFromMediaType(message.ContentType), message.Payload);

            if (observeQuery.Count() == 0)
            {
                var query = container.Where(c => c.Value.Item1 == Convert.ToBase64String(message.Token));
                KeyValuePair<ushort, Tuple<string, DateTime, Action<CodeType, string, byte[]>>>[]
                    kvps = query.ToArray();
                foreach (var kvp in kvps)
                {
                    kvp.Value.Item3(message.Code, MediaTypeConverter.ConvertFromMediaType(message.ContentType),
                        message.Payload);
                    container.Remove(kvp.Key);
                }
            }

            timer.Enabled = container.Count() > 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ushort NewId(byte[] token, bool? observe = null, Action<CodeType, string, byte[]> action = null)
        {
            if (observe.HasValue && observe.Value && action == null)
            {
                throw new ArgumentNullException("action");
            }

            currentId++;
            currentId = currentId == ushort.MaxValue ? (ushort)1 : currentId;

            while (container.ContainsKey(currentId))
            {
                currentId++;
                currentId = currentId == ushort.MaxValue ? (ushort)1 : currentId;
            }

            if (observe.HasValue && observe.Value)
            {
                observeContainer.Add(Convert.ToBase64String(token), action);
            }

            Tuple<string, DateTime, Action<CodeType, string, byte[]>> tuple =
                new Tuple<string, DateTime, Action<CodeType, string, byte[]>>(Convert.ToBase64String(token),
                    DateTime.UtcNow.AddMilliseconds(lifetimeMilliseconds), action);
            container.Add(currentId, tuple);

            timer.Enabled = true;

            return currentId;
        }

        public void Remove(ushort id)
        {
            container.Remove(id);
            retryContainer.Remove(id);

            timer.Enabled = container.Count > 0;
        }

        public void Unobserve(byte[] token)
        {
            string tokenString = Convert.ToBase64String(token);
            if (observeContainer.ContainsKey(tokenString))
            {
                observeContainer.Remove(tokenString);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Dispose();
                    }

                    observeContainer.Clear();
                    retryContainer.Clear();
                    container.Clear();
                }

                disposedValue = true;
            }
        }

        private void ManageRetries()
        {
            var retryQuery = retryContainer.Where(c => c.Value.Item1 < DateTime.UtcNow);

            if (retryQuery != null)
            {
                List<ushort> retryUpdateList = new List<ushort>();
                foreach (var item in retryQuery)
                {
                    OnRetry(this, new CoapMessageEventArgs(item.Value.Item3));
                    retryUpdateList.Add(item.Key);
                }

                List<ushort> retryRemoveList = new List<ushort>();
                List<KeyValuePair<ushort, Tuple<DateTime, int, CoapMessage>>> kvpList =
                    new List<KeyValuePair<ushort, Tuple<DateTime, int, CoapMessage>>>();
                foreach (var item in retryUpdateList)
                {
                    Tuple<DateTime, int, CoapMessage> tuple = retryContainer[item];
                    if (tuple.Item2 + 1 == maxAttempts - 1)
                    {
                        retryRemoveList.Add(item);
                    }
                    else
                    {
                        Tuple<DateTime, int, CoapMessage> t =
                            new Tuple<DateTime, int, CoapMessage>(tuple.Item1.AddMilliseconds(retryMilliseconds),
                                tuple.Item2 + 1, tuple.Item3);
                        kvpList.Add(new KeyValuePair<ushort, Tuple<DateTime, int, CoapMessage>>(item, t));
                    }
                }

                foreach (var item in retryRemoveList)
                    retryContainer.Remove(item);

                foreach (var item in kvpList)
                    retryContainer[item.Key] = item.Value;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var query = container.Where(c => c.Value.Item2 < DateTime.UtcNow);

            List<ushort> list = new List<ushort>();
            if (query != null && query.Count() > 0)
            {
                foreach (var item in query)
                    list.Add(item.Key);
            }

            foreach (var item in list)
            {
                container.Remove(item);
                if (retryContainer.ContainsKey(item))
                {
                    retryContainer.Remove(item);
                }
            }

            ManageRetries();

            timer.Enabled = container.Count() > 0;
        }
    }
}