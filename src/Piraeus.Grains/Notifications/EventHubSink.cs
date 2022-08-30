﻿using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.EventHubs;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;

namespace Piraeus.Grains.Notifications
{
    public class EventHubSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly int clientCount;

        private readonly string connectionString;

        private readonly string hubName;

        private readonly string keyName;

        private readonly string partitionId;

        private readonly ConcurrentQueue<byte[]> queue;

        private readonly PartitionSender[] senderArray;

        private readonly EventHubClient[] storageArray;

        private readonly Uri uri;

        private int arrayIndex;

        public EventHubSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            queue = new ConcurrentQueue<byte[]>();

            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            uri = new Uri(metadata.NotifyAddress);
            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            keyName = nvc["keyname"];
            partitionId = nvc["partitionid"];
            hubName = nvc["hub"];
            connectionString =
                $"Endpoint=sb://{uri.Authority}/;SharedAccessKeyName={keyName};SharedAccessKey={metadata.SymmetricKey}";

            if (!int.TryParse(nvc["clients"], out clientCount))
            {
                clientCount = 1;
            }

            if (!string.IsNullOrEmpty(partitionId))
            {
                senderArray = new PartitionSender[clientCount];
            }

            storageArray = new EventHubClient[clientCount];
            for (int i = 0; i < clientCount; i++)
            {
                storageArray[i] = EventHubClient.CreateFromConnectionString(connectionString);

                if (!string.IsNullOrEmpty(partitionId))
                {
                    senderArray[i] = storageArray[i].CreatePartitionSender(partitionId);
                }
            }
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;
            byte[] payload = null;

            try
            {
                byte[] msg = GetPayload(message);
                queue.Enqueue(msg);

                while (!queue.IsEmpty)
                {
                    arrayIndex = arrayIndex.RangeIncrement(0, clientCount - 1);
                    queue.TryDequeue(out payload);

                    if (payload == null)
                    {
                        await logger?.LogWarningAsync(
                            $"Subscription '{metadata.SubscriptionUriString}' message not written to event hub sink because message is null.");
                        return;
                    }

                    EventData data = new EventData(payload);
                    data.Properties.Add("Content-Type", message.ContentType);

                    if (string.IsNullOrEmpty(partitionId))
                    {
                        await storageArray[arrayIndex].SendAsync(data);
                    }
                    else
                    {
                        await senderArray[arrayIndex].SendAsync(data);
                    }

                    if (message.Audit && record != null)
                    {
                        record = new MessageAuditRecord(message.MessageId,
                            $"sb://{uri.Authority}/{hubName}", "EventHub", "EventHub",
                            payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
                    }
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to event grid hub sink.");
                record = new MessageAuditRecord(message.MessageId, string.Format("sb://{0}", uri.Authority, hubName),
                    "EventHub", "EventHub", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow,
                    ex.Message);
                throw;
            }
            finally
            {
                if (message.Audit && record != null)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        private byte[] GetPayload(EventMessage message)
        {
            switch (message.Protocol)
            {
                case ProtocolType.COAP:
                    CoapMessage coap = CoapMessage.DecodeMessage(message.Message);
                    return coap.Payload;

                case ProtocolType.MQTT:
                    MqttMessage mqtt = MqttMessage.DecodeMessage(message.Message);
                    return mqtt.Payload;

                case ProtocolType.REST:
                    return message.Message;

                case ProtocolType.WSN:
                    return message.Message;

                default:
                    return null;
            }
        }
    }
}