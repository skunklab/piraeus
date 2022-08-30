﻿using System.Threading.Tasks;

namespace SkunkLab.Protocols.Mqtt.Handlers
{
    public class MqttConnackHandler : MqttMessageHandler
    {
        public MqttConnackHandler(MqttSession session, MqttMessage message)
            : base(session, message)
        {
        }

        public override async Task<MqttMessage> ProcessAsync()
        {
            ConnectAckMessage msg = Message as ConnectAckMessage;
            Session.IsConnected = msg.ReturnCode == ConnectAckCode.ConnectionAccepted;
            Session.Connect(msg.ReturnCode);
            Session.IncrementKeepAlive();
            return await Task.FromResult<MqttMessage>(null);
        }
    }
}