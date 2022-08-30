﻿using System.Threading.Tasks;

namespace SkunkLab.Protocols.Coap.Handlers
{
    public abstract class CoapMessageHandler
    {
        protected CoapMessageHandler(CoapSession session, CoapMessage message, ICoapRequestDispatch dispatcher = null)
        {
            Session = session;
            Message = message;
            Dispatcher = dispatcher;
        }

        protected ICoapRequestDispatch Dispatcher
        {
            get; set;
        }

        protected CoapMessage Message
        {
            get; set;
        }

        protected CoapSession Session
        {
            get; set;
        }

        public static CoapMessageHandler Create(CoapSession session, CoapMessage message,
            ICoapRequestDispatch dispatcher = null)
        {
            if (message.Code == CodeType.EmptyMessage && message.MessageType == CoapMessageType.Confirmable)
            {
                return new CoapPingHandler(session, message);
            }

            if (message.Code == CodeType.POST)
            {
                return new CoapPostHandler(session, message, dispatcher);
            }

            if (message.Code == CodeType.PUT)
            {
                return new CoapPutHandler(session, message, dispatcher);
            }

            if (message.Code == CodeType.GET)
            {
                return new CoapObserveHandler(session, message, dispatcher);
            }

            if (message.Code == CodeType.DELETE)
            {
                return new CoapDeleteHandler(session, message, dispatcher);
            }

            if (message.MessageType == CoapMessageType.Reset)
            {
                return new CoapRstHandler(session, message);
            }

            return new CoapResponseHandler(session, message);
        }

        public abstract Task<CoapMessage> ProcessAsync();
    }
}