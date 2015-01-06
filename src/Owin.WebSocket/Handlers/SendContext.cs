using System;
using System.Net.WebSockets;

namespace Owin.WebSocket.Handlers
{
    internal class SendContext
    {
        public ArraySegment<byte> Buffer;
        public bool EndOfMessage;
        public WebSocketMessageType Type;

        public SendContext(ArraySegment<byte> buffer, bool endOfMessage, WebSocketMessageType type)
        {
            Buffer = buffer;
            EndOfMessage = endOfMessage;
            Type = type;
        }
    }
}