using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Fleck;
using Microsoft.Owin;

namespace Owin.WebSocket
{
    public interface IOwinWebSocketConnection : IWebSocketConnection
    {
        Func<Task> OnOpenAsync { get; set; }
        Action<WebSocketCloseStatus?, string> OnCloseOwin { get; set; }
        Func<WebSocketCloseStatus?, string, Task> OnCloseOwinAsync { get; set; }
        Func<IOwinRequest, bool> OnAuthenticateRequest { get; set; }
        Func<IOwinRequest, Task<bool>> OnAuthenticateRequestAsync { get; set; }
    }
}