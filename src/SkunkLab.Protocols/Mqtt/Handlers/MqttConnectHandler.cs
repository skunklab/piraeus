﻿using System.Threading.Tasks;

namespace SkunkLab.Protocols.Mqtt.Handlers
{
    public class MqttConnectHandler : MqttMessageHandler
    {
        public MqttConnectHandler(MqttSession session, MqttMessage message)
            : base(session, message)
        {
        }

        public override async Task<MqttMessage> ProcessAsync()
        {
            if (Session.IsConnected)
            {
                Session.Disconnect(Message);
                return null;
            }

            ConnectMessage msg = Message as ConnectMessage;

            if (msg.ProtocolVersion != 4)
            {
                Session.ConnectResult = ConnectAckCode.UnacceptableProtocolVersion;
                return await Task.FromResult<MqttMessage>(new ConnectAckMessage(false,
                    ConnectAckCode.UnacceptableProtocolVersion));
            }

            if (msg.ClientId == null && !msg.CleanSession)
            {
                Session.ConnectResult = ConnectAckCode.IdentifierRejected;
                return await Task.FromResult<MqttMessage>(new ConnectAckMessage(false,
                    ConnectAckCode.IdentifierRejected));
            }

            if (!Session.IsAuthenticated)
            {
                Session.ConnectResult = ConnectAckCode.NotAuthorized;
                return await Task.FromResult<MqttMessage>(new ConnectAckMessage(false,
                    ConnectAckCode.BadUsernameOrPassword));
            }

            Session.IsConnected = true;
            Session.Connect(ConnectAckCode.ConnectionAccepted);
            return await Task.FromResult<MqttMessage>(new ConnectAckMessage(false, ConnectAckCode.ConnectionAccepted));
        }
    }
}