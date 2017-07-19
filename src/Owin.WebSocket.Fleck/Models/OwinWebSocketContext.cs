using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace Owin.WebSocket.Models
{
    public class OwinWebSocketContext : IOwinWebSocketContext
    {
        private readonly IOwinContext _context;

        public OwinWebSocketContext(IOwinContext context, int maxMessageSize)
        {
            _context = context;
            MaxMessageSize = maxMessageSize;
        }

        public T Get<T>(string key)
        {
            return _context.Get<T>(key);
        }

        public IOwinContext Set<T>(string key, T value)
        {
            return _context.Set(key, value);
        }

        public IOwinRequest Request => _context.Request;
        public IOwinResponse Response => _context.Response;
        public IAuthenticationManager Authentication => _context.Authentication;
        public IDictionary<string, object> Environment => _context.Environment;

        public TextWriter TraceOutput
        {
            get { return _context.TraceOutput; }
            set { _context.TraceOutput = value; }
        }

        public int MaxMessageSize { get; }
        public WebSocketCloseStatus? CloseStatus { get; set; }
        public string CloseStatusDescription { get; set; }
    }
}