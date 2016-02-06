using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fleck;
using Microsoft.Owin.Security;

namespace Owin.WebSocket
{
    public interface IOwinWebSocketConnection : IWebSocketConnection
    {
        IOwinWebSocketContext Context { get; }
        Func<Task> OnOpenAsync { get; set; }
        Func<Task> OnCloseAsync { get; set; }
        Func<string, Task> OnMessageAsync { get; set; }
        Func<byte[], Task> OnBinaryAsync { get; set; }
        Func<bool> OnAuthenticateRequest { get; set; }
        Func<Task<bool>> OnAuthenticateRequestAsync { get; set; }
    }
}