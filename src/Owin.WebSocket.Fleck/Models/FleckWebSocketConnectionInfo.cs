using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;
using Microsoft.Owin;

namespace Owin.WebSocket.Models
{
    public class FleckWebSocketConnectionInfo : IWebSocketConnectionInfo
    {
        private readonly IOwinContext _context;

        public FleckWebSocketConnectionInfo(IOwinContext context)
        {
            _context = context;
        }

        string IWebSocketConnectionInfo.SubProtocol
        {
            get
            {
                const string protocol = "Sec-WebSocket-Protocol";
                return _context.Request.Headers.ContainsKey(protocol)
                    ? _context.Request.Headers[protocol]
                    : string.Empty;
            }
        }

        string IWebSocketConnectionInfo.Origin
        {
            get
            {
                const string origin1 = "Origin";
                if (_context.Request.Headers.ContainsKey(origin1))
                    return _context.Request.Headers[origin1];

                const string origin2 = "Sec-WebSocket-Origin";
                return _context.Request.Headers.ContainsKey(origin2)
                    ? _context.Request.Headers[origin2]
                    : string.Empty;
            }
        }

        string IWebSocketConnectionInfo.Host => _context.Request.Host.Value;
        string IWebSocketConnectionInfo.Path => _context.Request.Path.Value;
        string IWebSocketConnectionInfo.ClientIpAddress => _context.Request.RemoteIpAddress;
        int IWebSocketConnectionInfo.ClientPort => _context.Request.RemotePort ?? 0;
        IDictionary<string, string> IWebSocketConnectionInfo.Cookies => _context.Request.Cookies.ToDictionary(k => k.Key, v => v.Value);
        IDictionary<string, string> IWebSocketConnectionInfo.Headers => _context.Request.Headers.SelectMany(p => p.Value.Select(v => new { p.Key, Value = v })).ToDictionary(k => k.Key, v => v.Value);

        Guid IWebSocketConnectionInfo.Id { get; } = Guid.NewGuid();
        string IWebSocketConnectionInfo.NegotiatedSubProtocol { get; } = string.Empty;
    }
}