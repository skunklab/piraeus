﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Orleans;
using Piraeus.Adapters.Utilities;
using Piraeus.Auditing;
using Piraeus.Configuration;
using Piraeus.Core;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.Grains;
using SkunkLab.Channels;
using SkunkLab.Channels.Udp;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Protocols.Mqtt.Handlers;
using SkunkLab.Security.Authentication;
using SkunkLab.Security.Identity;

namespace Piraeus.Adapters
{
    public class MqttProtocolAdapter : ProtocolAdapter
    {
        private readonly PiraeusConfig config;

        private readonly HttpContext context;

        private readonly GraphManager graphManager;

        private readonly ILog logger;

        private readonly MqttSession session;

        private OrleansAdapter adapter;

        private IAuditFactory auditFactory;

        private bool closing;

        private bool disposed;

        private bool forcePerReceiveAuthn;

        private IAuditor messageAuditor;

        private IAuditor userAuditor;

        public MqttProtocolAdapter(PiraeusConfig config, GraphManager graphManager, IAuthenticator authenticator,
            IChannel channel, ILog logger, HttpContext context = null)
        {
            this.config = config;
            this.graphManager = graphManager;
            this.logger = logger;

            MqttConfig mqttConfig = new MqttConfig(config.KeepAliveSeconds, config.AckTimeoutSeconds,
                config.AckRandomFactor, config.MaxRetransmit, config.MaxLatencySeconds, authenticator,
                config.ClientIdentityNameClaimType, config.GetClientIndexes());

            this.context = context;

            InitializeAuditor(config);

            session = new MqttSession(mqttConfig);

            Channel = channel;
            Channel.OnClose += Channel_OnClose;
            Channel.OnError += Channel_OnError;
            Channel.OnStateChange += Channel_OnStateChange;
            Channel.OnReceive += Channel_OnReceive;
            Channel.OnOpen += Channel_OnOpen;
        }

        public override event System.EventHandler<ProtocolAdapterCloseEventArgs> OnClose;

        public override event System.EventHandler<ProtocolAdapterErrorEventArgs> OnError;

        public override event System.EventHandler<ChannelObserverEventArgs> OnObserve;

        public override IChannel Channel
        {
            get; set;
        }

        public override void Init()
        {
            auditFactory = AuditFactory.CreateSingleton();
            if (config.AuditConnectionString != null &&
                config.AuditConnectionString.Contains("DefaultEndpointsProtocol"))
            {
                auditFactory.Add(new AzureTableAuditor(config.AuditConnectionString, "messageaudit"),
                    AuditType.Message);
                auditFactory.Add(new AzureTableAuditor(config.AuditConnectionString, "useraudit"), AuditType.User);
            }
            else if (config.AuditConnectionString != null)
            {
                auditFactory.Add(new FileAuditor(config.AuditConnectionString), AuditType.Message);
                auditFactory.Add(new FileAuditor(config.AuditConnectionString), AuditType.User);
            }

            messageAuditor = auditFactory.GetAuditor(AuditType.Message);
            userAuditor = auditFactory.GetAuditor(AuditType.User);
            logger?.LogDebugAsync("MQTT adapter audit factory added.").GetAwaiter();

            forcePerReceiveAuthn = Channel as UdpChannel != null;
            session.OnPublish += Session_OnPublish;
            session.OnSubscribe += Session_OnSubscribe;
            session.OnUnsubscribe += Session_OnUnsubscribe;
            session.OnDisconnect += Session_OnDisconnect;
            ;
            session.OnConnect += Session_OnConnect;
            logger?.LogInformationAsync($"MQTT adpater on channel '{Channel.Id}' is initialized.").GetAwaiter();
        }

        private void InitializeAuditor(PiraeusConfig config)
        {
            if (!string.IsNullOrEmpty(config.AuditConnectionString) &&
                AuditFactory.CreateSingleton().GetAuditor(AuditType.User) == null)
            {
                auditFactory = AuditFactory.CreateSingleton();

                if (config.AuditConnectionString.ToLowerInvariant().Contains("AccountName="))
                {
                    auditFactory.Add(new AzureTableAuditor(config.AuditConnectionString, "messageaudit"),
                        AuditType.Message);
                    auditFactory.Add(new AzureTableAuditor(config.AuditConnectionString, "useraudit"), AuditType.User);
                }
                else
                {
                    string pathString =
                        config.AuditConnectionString.LastIndexOf("/") == config.AuditConnectionString.Length - 1
                            ? config.AuditConnectionString
                            : config.AuditConnectionString + "/";
                    auditFactory.Add(new FileAuditor(string.Format($"{pathString}messageaudit.txt")),
                        AuditType.Message);
                    auditFactory.Add(new FileAuditor(string.Format($"{pathString}useraudit.txt")), AuditType.User);
                }
            }
        }

        #region Orleans Adapter Events

        private void Adapter_OnObserve(object sender, ObserveMessageEventArgs e)
        {
            MessageAuditRecord record = null;
            int length = 0;
            DateTime sendTime = DateTime.UtcNow;
            try
            {
                byte[] message = ProtocolTransition.ConvertToMqtt(session, e.Message);
                Send(message).LogExceptions();

                MqttMessage mm = MqttMessage.DecodeMessage(message);

                length = mm.Payload.Length;
                record = new MessageAuditRecord(e.Message.MessageId, session.Identity, Channel.TypeId, "MQTT", length,
                    MessageDirectionType.Out, true, sendTime);
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0} - MQTT adapter observe error on channel '{1}' with '{2}'",
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH-MM-ss.fffff"), Channel.Id, ex.Message);
                logger?.LogErrorAsync(ex, $"MQTT adapter observe error on channel '{Channel.Id}'.").GetAwaiter();
                record = new MessageAuditRecord(e.Message.MessageId, session.Identity, Channel.TypeId, "MQTT", length,
                    MessageDirectionType.Out, true, sendTime, msg);
            }
            finally
            {
                if (e.Message.Audit)
                {
                    messageAuditor?.WriteAuditRecordAsync(record).Ignore();
                }
            }
        }

        private async Task Send(byte[] message)
        {
            try
            {
                await Channel.SendAsync(message);
                await logger?.LogDebugAsync("MQTT adapter sent message on channel");
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"MQTT adapter send error on channel '{Channel.Id}'.");
            }
        }

        #endregion Orleans Adapter Events

        #region MQTT Session Events

        private List<KeyValuePair<string, string>> GetIndexes(MqttUri mqttUri)
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>(mqttUri.Indexes);

            if (mqttUri.Indexes.Contains(new KeyValuePair<string, string>("~", "~")))
            {
                list.Remove(new KeyValuePair<string, string>("~", "~"));
                var query = config.GetClientIndexes().Where(ck => ck.Key == session.Config.IdentityClaimType);
                if (query.Count() == 1)
                {
                    query.GetEnumerator().MoveNext();
                    list.Add(new KeyValuePair<string, string>(query.GetEnumerator().Current.Value,
                        "~" + session.Identity));
                }
            }

            return list.Count > 0 ? list : null;
        }

        private async Task PublishAsync(PublishMessage message)
        {
            MessageAuditRecord record = null;
            EventMetadata metadata = null;

            try
            {
                MqttUri mqttUri = new MqttUri(message.Topic);
                metadata = await graphManager.GetPiSystemMetadataAsync(mqttUri.Resource);
                if (EventValidator.Validate(true, metadata, Channel, graphManager, context).Validated)
                {
                    EventMessage msg = new EventMessage(mqttUri.ContentType, mqttUri.Resource, ProtocolType.MQTT,
                        message.Encode(), DateTime.UtcNow, metadata.Audit);
                    if (!string.IsNullOrEmpty(mqttUri.CacheKey))
                    {
                        msg.CacheKey = mqttUri.CacheKey;
                    }

                    if (mqttUri.Indexes != null)
                    {
                        List<KeyValuePair<string, string>> list = GetIndexes(mqttUri);
                        await adapter.PublishAsync(msg, list);
                    }
                    else
                    {
                        await adapter.PublishAsync(msg);
                    }
                }
                else
                {
                    if (metadata.Audit)
                    {
                        record = new MessageAuditRecord("XXXXXXXXXXXX", session.Identity, Channel.TypeId, "MQTT",
                            message.Payload.Length, MessageDirectionType.In, false, DateTime.UtcNow,
                            "Not authorized, missing resource metadata, or channel encryption requirements");
                    }

                    throw new SecurityException(string.Format("'{0}' not authorized to publish to '{1}'",
                        session.Identity, metadata.ResourceUriString));
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"MQTT adapter PublishAsync error on channel '{Channel.Id}'.");
                OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, ex));
            }
            finally
            {
                if (metadata != null && metadata.Audit && record != null)
                {
                    await messageAuditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        private void Session_OnConnect(object sender, MqttConnectionArgs args)
        {
            try
            {
                logger?.LogDebugAsync($"MQTT adapter connnected on channel '{Channel.Id}'.").GetAwaiter();
                adapter.LoadDurableSubscriptionsAsync(session.Identity).GetAwaiter();
            }
            catch (Exception ex)
            {
                logger?.LogErrorAsync(ex, $"MQTT adapter Session_OnConnect error on channel '{Channel.Id}'.")
                    .GetAwaiter();
                OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, ex));
            }
        }

        private void Session_OnDisconnect(object sender, MqttMessageEventArgs args)
        {
            logger?.LogDebugAsync($"MQTT adapter disconnected on channel '{Channel.Id}'.").GetAwaiter();
            OnError?.Invoke(this,
                new ProtocolAdapterErrorEventArgs(Channel.Id,
                    new DisconnectException(string.Format("MQTT adapter on channel {0} has been disconnected.",
                        Channel.Id))));
        }

        private void Session_OnPublish(object sender, MqttMessageEventArgs args)
        {
            try
            {
                PublishMessage message = args.Message as PublishMessage;
                MqttUri muri = new MqttUri(message.Topic);
                PublishAsync(message).GetAwaiter();
            }
            catch (Exception ex)
            {
                logger?.LogErrorAsync(ex, $"MQTT adapter Session_OnPublish  error on channel '{Channel.Id}'.")
                    .GetAwaiter();
                OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, ex));
            }
        }

        private List<string> Session_OnSubscribe(object sender, MqttMessageEventArgs args)
        {
            List<string> list = new List<string>();

            try
            {
                SubscribeMessage message = args.Message as SubscribeMessage;

                SubscriptionMetadata metadata = new SubscriptionMetadata
                {
                    Identity = session.Identity,
                    Indexes = session.Indexes,
                    IsEphemeral = true
                };

                foreach (var item in message.Topics)
                {
                    MqttUri uri = new MqttUri(item.Key);
                    string resourceUriString = uri.Resource;

                    if (EventValidator.Validate(false, resourceUriString, Channel, graphManager, context).Validated)
                    {
                        Task<string> subTask = Subscribe(resourceUriString, metadata);
                        string subscriptionUriString = subTask.Result;
                        list.Add(resourceUriString);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogErrorAsync(ex, $"MQTT adapter Session_OnSubscribe error on channel '{Channel.Id}'.")
                    .GetAwaiter();
                OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, ex));
            }

            return list;
        }

        private void Session_OnUnsubscribe(object sender, MqttMessageEventArgs args)
        {
            try
            {
                UnsubscribeMessage msg = (UnsubscribeMessage)args.Message;
                foreach (var item in msg.Topics)
                {
                    MqttUri uri = new MqttUri(item.ToLowerInvariant());

                    if (EventValidator.Validate(false, uri.Resource, Channel, graphManager, context).Validated)
                    {
                        adapter.UnsubscribeAsync(uri.Resource).GetAwaiter();
                        logger?.LogInformationAsync($"MQTT adapter unsubscribed {uri}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogErrorAsync(ex, $"MQTT adapter Session_OnUnsubscribe error on channel '{Channel.Id}'.")
                    .GetAwaiter();
                OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, ex));
            }
        }

        private Task<string> Subscribe(string resourceUriString, SubscriptionMetadata metadata)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            Task t = Task.Factory.StartNew(async () =>
            {
                try
                {
                    string id = await adapter.SubscribeAsync(resourceUriString, metadata);
                    tcs.SetResult(id);
                }
                catch (Exception ex)
                {
                    await logger?.LogErrorAsync(ex, $"MQTT adapter Subscribe error on channel '{Channel.Id}'.");
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        #endregion MQTT Session Events

        #region Channel Events

        private void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            try
            {
                if (!closing)
                {
                    closing = true;
                    UserAuditRecord record = new UserAuditRecord(Channel.Id, session.Identity, DateTime.UtcNow);
                    userAuditor?.UpdateAuditRecordAsync(record).IgnoreException();
                }

                OnClose?.Invoke(this, new ProtocolAdapterCloseEventArgs(e.ChannelId));
            }
            catch
            {
            }
        }

        private void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            logger?.LogErrorAsync(e.Error, $"MQTT adapter Channel_OnError error on channel '{Channel.Id}'.")
                .GetAwaiter();
            OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, e.Error));
        }

        private void Channel_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            try
            {
                session.IsAuthenticated = Channel.IsAuthenticated;
                if (session.IsAuthenticated)
                {
                    IdentityDecoder decoder = new IdentityDecoder(session.Config.IdentityClaimType, context,
                        session.Config.Indexes);
                    session.Identity = decoder.Id;
                    session.Indexes = decoder.Indexes;

                    UserAuditRecord record = new UserAuditRecord(Channel.Id, session.Identity,
                        session.Config.IdentityClaimType, Channel.TypeId, "MQTT", "Granted", DateTime.UtcNow);
                    userAuditor?.WriteAuditRecordAsync(record).Ignore();
                }

                adapter = new OrleansAdapter(session.Identity, Channel.TypeId, "MQTT", graphManager, logger);
                adapter.OnObserve += Adapter_OnObserve;
            }
            catch (Exception ex)
            {
                logger?.LogErrorAsync(ex, $"MQTT adapter Channel_OnOpen error on channel '{Channel.Id}'.").GetAwaiter();
                OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, ex));
            }
        }

        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            try
            {
                MqttMessage msg = MqttMessage.DecodeMessage(e.Message);
                OnObserve?.Invoke(this, new ChannelObserverEventArgs(Channel.Id, null, null, e.Message));

                if (!session.IsAuthenticated)
                {
                    if (!(msg is ConnectMessage message))
                    {
                        throw new SecurityException("Connect message not first message");
                    }

                    if (session.Authenticate(message.Username, message.Password))
                    {
                        IdentityDecoder decoder = new IdentityDecoder(session.Config.IdentityClaimType, context,
                            session.Config.Indexes);
                        session.Identity = decoder.Id;
                        session.Indexes = decoder.Indexes;
                        adapter.Identity = decoder.Id;

                        UserAuditRecord record = new UserAuditRecord(Channel.Id, session.Identity,
                            session.Config.IdentityClaimType, Channel.TypeId, "MQTT", "Granted", DateTime.UtcNow);
                        userAuditor?.WriteAuditRecordAsync(record).Ignore();
                    }
                    else
                    {
                        throw new SecurityException("Session could not be authenticated.");
                    }
                }
                else if (forcePerReceiveAuthn)
                {
                    if (!session.Authenticate())
                    {
                        throw new SecurityException("Per receive authentication failed.");
                    }
                }

                ProcessMessageAsync(msg).GetAwaiter();
            }
            catch (Exception ex)
            {
                logger?.LogErrorAsync(ex, $"MQTT adapter Channel_OnReceive error on channel '{Channel.Id}'.")
                    .GetAwaiter();
                OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, ex));
            }
        }

        private void Channel_OnStateChange(object sender, ChannelStateEventArgs e)
        {
            logger?.LogInformationAsync($"MQTT adapter Channel_OnStateChange to '{e.State}' on channel '{Channel.Id}'.")
                .GetAwaiter();
        }

        private async Task ProcessMessageAsync(MqttMessage message)
        {
            try
            {
                MqttMessageHandler handler = MqttMessageHandler.Create(session, message);
                MqttMessage msg = await handler.ProcessAsync();

                if (msg != null)
                {
                    await Channel.SendAsync(msg.Encode());
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"MQTT adapter ProcessMessageAsync error on channel '{Channel.Id}'.");
                OnError.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, ex));
            }
        }

        #endregion Channel Events

        #region Dispose

        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        protected void Disposing(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (adapter != null)
                        {
                            adapter.Dispose();
                            logger?.LogDebugAsync($"MQTT adapter disposed on channel {Channel.Id}").GetAwaiter();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogErrorAsync(ex,
                            $"MQTT adapter disposing orleans adapter error on channel '{Channel.Id}'.").GetAwaiter();
                    }

                    try
                    {
                        if (Channel != null)
                        {
                            string channelId = Channel.Id;
                            Channel.Dispose();
                            logger?.LogDebugAsync($"MQTT adapter channel {channelId} disposed").GetAwaiter();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogErrorAsync(ex, $"MQTT adapter Disposing channel on channel '{Channel.Id}'.")
                            .GetAwaiter();
                    }

                    try
                    {
                        if (session != null)
                        {
                            session.Dispose();
                            logger?.LogDebugAsync("MQTT adapter disposed session.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogErrorAsync(ex, $"MQTT adapter Disposing session on channel '{Channel.Id}'.")
                            .GetAwaiter();
                    }
                }

                disposed = true;
            }
        }

        #endregion Dispose
    }
}