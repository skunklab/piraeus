﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkunkLab.Channels;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Coap.Handlers;
using SkunkLab.Security.Tokens;

namespace SkunkLab.Clients.Coap
{
    public class PiraeusCoapClient
    {
        private readonly IChannel channel;

        private readonly CoapConfig config;

        private readonly ICoapRequestDispatch dispatcher;

        private readonly Dictionary<string, string> observers;

        private readonly List<ushort> pingId;

        private readonly Queue<byte[]> queue;

        private readonly string securityToken;

        private readonly CoapSession session;

        private readonly SecurityTokenType tokenType;

        private bool usedToken;

        public PiraeusCoapClient(CoapConfig config, IChannel channel, SecurityTokenType tokenType, string securityToken,
            ICoapRequestDispatch dispatcher = null)
            : this(config, channel, dispatcher)
        {
            this.tokenType = tokenType;
            this.securityToken = securityToken;
            usedToken = false;
        }

        public PiraeusCoapClient(CoapConfig config, IChannel channel, ICoapRequestDispatch dispatcher = null)
        {
            this.config = config;
            pingId = new List<ushort>();
            session = new CoapSession(config);

            observers = new Dictionary<string, string>();
            this.dispatcher = dispatcher;
            this.channel = channel;
            this.channel.OnClose += Channel_OnClose;
            this.channel.OnError += Channel_OnError;
            this.channel.OnOpen += Channel_OnOpen;
            this.channel.OnStateChange += Channel_OnStateChange;
            this.channel.OnReceive += Channel_OnReceive;
            session.OnRetry += Session_OnRetry;
            session.OnKeepAlive += Session_OnKeepAlive;
            session.IsAuthenticated = true;
            usedToken = true;
            queue = new Queue<byte[]>();
        }

        public event System.EventHandler<CoapMessageEventArgs> OnPingResponse;

        public async Task ObserveAsync(string resourceUriString, Action<CodeType, string, byte[]> action)
        {
            if (!channel.IsConnected)
            {
                await ConnectAsync();
            }

            session.UpdateKeepAliveTimestamp();

            byte[] token = CoapToken.Create().TokenBytes;
            ushort id = session.CoapSender.NewId(token, true, action);
            string scheme = channel.IsEncrypted ? "coaps" : "coap";
            string coapUriString = GetCoapUriString(scheme, resourceUriString);
            CoapRequest cr = new CoapRequest(id, RequestMessageType.NonConfirmable, MethodType.GET, token,
                new Uri(coapUriString), null)
            {
                Observe = true
            };
            observers.Add(resourceUriString, Convert.ToBase64String(token));
            byte[] observeRequest = cr.Encode();
            await channel.SendAsync(observeRequest);
        }

        public async Task PublishAsync(string resourceUriString, string contentType, byte[] payload, bool confirmable,
            Action<CodeType, string, byte[]> action)
        {
            if (!channel.IsConnected)
            {
                await ConnectAsync();
            }

            session.UpdateKeepAliveTimestamp();

            byte[] token = CoapToken.Create().TokenBytes;
            ushort id = session.CoapSender.NewId(token, null, action);
            string scheme = channel.IsEncrypted ? "coaps" : "coap";
            string coapUriString = GetCoapUriString(scheme, resourceUriString);

            RequestMessageType mtype = confirmable ? RequestMessageType.Confirmable : RequestMessageType.NonConfirmable;
            CoapRequest cr = new CoapRequest(id, mtype, MethodType.POST, token, new Uri(coapUriString),
                MediaTypeConverter.ConvertToMediaType(contentType), payload);

            queue.Enqueue(cr.Encode());

            while (queue.Count > 0)
            {
                byte[] message = queue.Dequeue();
                Task t = channel.SendAsync(message);
                await Task.WhenAll(t);
            }
        }

        public Task PublishAsync(string resourceUriString, string contentType, byte[] payload, NoResponseType nrt)
        {
            if (!channel.IsConnected)
            {
                Open();
                Receive();
            }

            session.UpdateKeepAliveTimestamp();

            byte[] token = CoapToken.Create().TokenBytes;
            ushort id = session.CoapSender.NewId(token);
            string scheme = channel.IsEncrypted ? "coaps" : "coap";
            string coapUriString = GetCoapUriString(scheme, resourceUriString);

            CoapRequest cr = new CoapRequest(id, RequestMessageType.NonConfirmable, MethodType.POST,
                new Uri(coapUriString), MediaTypeConverter.ConvertToMediaType(contentType), payload)
            {
                NoResponse = nrt
            };
            return channel.SendAsync(cr.Encode());
        }

        public async Task SubscribeAsync(string resourceUriString, bool confirmable,
            Action<CodeType, string, byte[]> action)
        {
            if (!channel.IsConnected)
            {
                await ConnectAsync();
            }

            session.UpdateKeepAliveTimestamp();

            byte[] token = CoapToken.Create().TokenBytes;
            ushort id = session.CoapSender.NewId(token, null, action);
            string scheme = channel.IsEncrypted ? "coaps" : "coap";
            string coapUriString = GetCoapUriString(scheme, resourceUriString);

            RequestMessageType mtype = confirmable ? RequestMessageType.Confirmable : RequestMessageType.NonConfirmable;
            CoapRequest cr = new CoapRequest(id, mtype, MethodType.PUT, token, new Uri(coapUriString), null);
            await channel.SendAsync(cr.Encode());
        }

        public async Task SubscribeAsync(string resourceUriString, NoResponseType nrt)
        {
            if (!channel.IsConnected)
            {
                await ConnectAsync();
            }

            session.UpdateKeepAliveTimestamp();

            byte[] token = CoapToken.Create().TokenBytes;
            ushort id = session.CoapSender.NewId(token);
            string scheme = channel.IsEncrypted ? "coaps" : "coap";
            string coapUriString = GetCoapUriString(scheme, resourceUriString);
            CoapRequest cr = new CoapRequest(id, RequestMessageType.NonConfirmable, MethodType.PUT, token,
                new Uri(coapUriString), null)
            {
                NoResponse = nrt
            };
            await channel.SendAsync(cr.Encode());
        }

        public async Task UnobserveAsync(string resourceUriString)
        {
            if (!channel.IsConnected)
            {
                await ConnectAsync();
            }

            session.UpdateKeepAliveTimestamp();

            if (observers.ContainsKey(resourceUriString))
            {
                string tokenString = observers[resourceUriString];
                byte[] token = Convert.FromBase64String(tokenString);
                ushort id = session.CoapSender.NewId(token, false);
                string scheme = channel.IsEncrypted ? "coaps" : "coap";
                string coapUriString = GetCoapUriString(scheme, resourceUriString);

                CoapRequest request = new CoapRequest(id, RequestMessageType.NonConfirmable, MethodType.GET,
                    new Uri(coapUriString), null)
                {
                    Observe = false
                };
                await channel.SendAsync(request.Encode());

                session.CoapSender.Unobserve(Convert.FromBase64String(tokenString));
            }
        }

        public async Task UnsubscribeAsync(string resourceUriString, bool confirmable,
            Action<CodeType, string, byte[]> action)
        {
            if (!channel.IsConnected)
            {
                await ConnectAsync();
            }

            session.UpdateKeepAliveTimestamp();

            byte[] token = CoapToken.Create().TokenBytes;
            ushort id = session.CoapSender.NewId(token, null, action);
            string scheme = channel.IsEncrypted ? "coaps" : "coap";
            string coapUriString = GetCoapUriString(scheme, resourceUriString);
            RequestMessageType mtype = confirmable ? RequestMessageType.Confirmable : RequestMessageType.NonConfirmable;
            CoapRequest cr = new CoapRequest(id, mtype, MethodType.DELETE, token, new Uri(coapUriString), null);
            await channel.SendAsync(cr.Encode());
        }

        public async Task UnsubscribeAsync(string resourceUriString, NoResponseType nrt)
        {
            if (!channel.IsConnected)
            {
                await ConnectAsync();
            }

            session.UpdateKeepAliveTimestamp();

            byte[] token = CoapToken.Create().TokenBytes;
            ushort id = session.CoapSender.NewId(token);
            string scheme = channel.IsEncrypted ? "coaps" : "coap";
            string coapUriString = GetCoapUriString(scheme, resourceUriString);
            CoapRequest cr = new CoapRequest(id, RequestMessageType.NonConfirmable, MethodType.DELETE, token,
                new Uri(coapUriString), null)
            {
                NoResponse = nrt
            };
            await channel.SendAsync(cr.Encode());
        }

        private async Task ConnectAsync()
        {
            try
            {
                await channel.OpenAsync();
                Receive();
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        private string GetCoapUriString(string scheme, string resourceUriString)
        {
            if (!usedToken && securityToken != null &&
                (tokenType != SecurityTokenType.NONE || tokenType != SecurityTokenType.X509))
            {
                usedToken = true;
                return string.Format("{0}://{1}?r={2}&tt={3}&t={4}", scheme, config.Authority, resourceUriString,
                    tokenType.ToString(), securityToken);
            }

            return string.Format("{0}://{1}?r={2}", scheme, config.Authority, resourceUriString);
        }

        private Task Open()
        {
            TaskCompletionSource<Task> tcs = new TaskCompletionSource<Task>();
            channel.OpenAsync().GetAwaiter();
            tcs.SetResult(null);
            return tcs.Task;
        }

        private void Receive()
        {
            Task task = channel.ReceiveAsync();
            Task.WhenAll(task);
        }

        private void Session_OnKeepAlive(object sender, CoapMessageEventArgs args)
        {
            Task task = channel.SendAsync(args.Message.Encode());
            Task.WhenAll(task);
        }

        private void Session_OnRetry(object sender, CoapMessageEventArgs args)
        {
            pingId.Add(args.Message.MessageId);
            Task task = channel.SendAsync(args.Message.Encode());
            Task.WhenAll(task);
        }

        #region channel events

        private void Channel_OnClose(object sender, ChannelCloseEventArgs args)
        {
        }

        private void Channel_OnError(object sender, ChannelErrorEventArgs args)
        {
        }

        private void Channel_OnOpen(object sender, ChannelOpenEventArgs args)
        {
        }

        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs args)
        {
            CoapMessage message = CoapMessage.DecodeMessage(args.Message);
            CoapMessageHandler handler = CoapMessageHandler.Create(session, message, dispatcher);

            Task task = ReceiveAsync(handler);
            Task.WhenAll(task);
        }

        private void Channel_OnStateChange(object sender, ChannelStateEventArgs args)
        {
        }

        private async Task ReceiveAsync(CoapMessageHandler handler)
        {
            CoapMessage msg = await handler.ProcessAsync();

            if (msg != null && pingId.Contains(msg.MessageId))
            {
                pingId.Remove(msg.MessageId);
                OnPingResponse?.Invoke(this, new CoapMessageEventArgs(msg));
            }
        }

        #endregion channel events
    }
}