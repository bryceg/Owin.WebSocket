using System.Net.WebSockets;
using Microsoft.Owin;

namespace Owin.WebSocket
{
    public interface IOwinWebSocketContext : IOwinContext
    {
        int MaxMessageSize { get; }
        WebSocketCloseStatus? CloseStatus { get; }
        string CloseStatusDescription { get; }
    }
}