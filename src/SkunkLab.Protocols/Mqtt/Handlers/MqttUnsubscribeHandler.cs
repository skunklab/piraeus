﻿using System.Threading.Tasks;

namespace SkunkLab.Protocols.Mqtt.Handlers
{
    public class MqttUnsubscribeHandler : MqttMessageHandler
    {
        public MqttUnsubscribeHandler(MqttSession session, MqttMessage message)
            : base(session, message)
        {
        }

        public override async Task<MqttMessage> ProcessAsync()
        {
            if (!Session.IsConnected)
            {
                Session.Disconnect(Message);
                return null;
            }

            Session.IncrementKeepAlive();
            Session.Unsubscribe(Message);
            return await Task.FromResult<MqttMessage>(new UnsubscribeAckMessage(Message.MessageId));
        }
    }
}