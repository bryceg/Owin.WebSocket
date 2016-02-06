using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace Owin.WebSocket
{
    public sealed class FleckWebSocketConnection : WebSocketConnection, IOwinWebSocketConnection, IWebSocketConnectionInfo
    {
        private const string PingPongError = "Owin handles ping pong messages internally";

        private readonly OwinWebSocketContext _context;
        private readonly IOwinWebSocketConnection _connection;

        private bool _isAvailable;
        
        public FleckWebSocketConnection()
        {
            _connection = this;
            _context = new OwinWebSocketContext(_connection.Context, MaxMessageSize);
        }

        #region WebSocketConnection

        public override bool AuthenticateRequest(IOwinRequest request)
        {
            return _connection.OnAuthenticateRequest?.Invoke() ?? true;
        }

        public override Task<bool> AuthenticateRequestAsync(IOwinRequest request)
        {
            return _connection.OnAuthenticateRequestAsync?.Invoke() ?? Task.FromResult(true);
        }

        public override void OnOpen()
        {
            _isAvailable = true;
            _connection.OnOpen?.Invoke();
        }

        public override Task OnOpenAsync()
        {
            return _connection.OnOpenAsync?.Invoke() ?? Task.FromResult(true);
        }
        
        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            _isAvailable = false;
            _context.CloseStatus = closeStatus;
            _context.CloseStatusDescription = closeStatusDescription;
            _connection.OnClose?.Invoke();
        }

        public override Task OnCloseAsync(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            return _connection.OnCloseAsync?.Invoke() ?? Task.FromResult(true);
        }

        public override Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            if (type == WebSocketMessageType.Binary && (_connection.OnBinary != null || _connection.OnBinaryAsync != null))
            {
                var array = message.ToArray();
                _connection.OnBinary?.Invoke(array);
                return _connection.OnBinaryAsync?.Invoke(array) ?? Task.FromResult(true);
            }

            if (type == WebSocketMessageType.Text && (_connection.OnMessage != null || _connection.OnMessageAsync!= null))
            {
                var @string = Encoding.UTF8.GetString(message.Array, 0, message.Count);
                _connection.OnMessage?.Invoke(@string);
                return _connection.OnMessageAsync?.Invoke(@string) ?? Task.FromResult(true);
            }

            return Task.FromResult(true);
        }

        public override void OnReceiveError(Exception error)
        {
            _connection.OnError?.Invoke(error);
        }

        #endregion

        #region IOwinWebSocketConnection

        IOwinWebSocketContext IOwinWebSocketConnection.Context => _context;
        Func<Task> IOwinWebSocketConnection.OnOpenAsync { get; set; }
        Func<Task> IOwinWebSocketConnection.OnCloseAsync { get; set; }
        Func<string, Task> IOwinWebSocketConnection.OnMessageAsync { get; set; }
        Func<byte[], Task> IOwinWebSocketConnection.OnBinaryAsync { get; set; }
        Func<bool> IOwinWebSocketConnection.OnAuthenticateRequest { get; set; }
        Func<Task<bool>> IOwinWebSocketConnection.OnAuthenticateRequestAsync { get; set; }

        #endregion

        #region IWebSocketConnection

        Action IWebSocketConnection.OnOpen { get; set; }
        Action IWebSocketConnection.OnClose { get; set; }
        Action<string> IWebSocketConnection.OnMessage { get; set; }
        Action<byte[]> IWebSocketConnection.OnBinary { get; set; }
        Action<Exception> IWebSocketConnection.OnError { get; set; }

        Action<byte[]> IWebSocketConnection.OnPing
        {
            get { return null; }
            set { throw new NotSupportedException(PingPongError); }
        }

        Action<byte[]> IWebSocketConnection.OnPong
        {
            get { return null; }
            set { throw new NotSupportedException(PingPongError); }
        }

        IWebSocketConnectionInfo IWebSocketConnection.ConnectionInfo => this;
        bool IWebSocketConnection.IsAvailable => _isAvailable;

        Task IWebSocketConnection.Send(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            return SendText(bytes, true);
        }

        Task IWebSocketConnection.Send(byte[] message)
        {
            return SendBinary(message, true);
        }

        Task IWebSocketConnection.SendPing(byte[] message)
        {
            throw new NotSupportedException(PingPongError);
        }

        Task IWebSocketConnection.SendPong(byte[] message)
        {
            throw new NotSupportedException(PingPongError);
        }

        void IWebSocketConnection.Close()
        {
            Close(WebSocketCloseStatus.NormalClosure, string.Empty).Wait();
        }

        #endregion

        #region IWebSocketConnectionInfo

        string IWebSocketConnectionInfo.SubProtocol
        {
            get
            {
                const string protocol = "Sec-WebSocket-Protocol";
                return Context.Request.Headers.ContainsKey(protocol) 
                    ? Context.Request.Headers[protocol] 
                    : string.Empty;
            }
        }

        string IWebSocketConnectionInfo.Origin
        {
            get
            {
                const string origin1 = "Origin";
                if (Context.Request.Headers.ContainsKey(origin1))
                    return Context.Request.Headers[origin1];

                const string origin2 = "Sec-WebSocket-Origin";
                return Context.Request.Headers.ContainsKey(origin2) 
                    ? Context.Request.Headers[origin2] 
                    : string.Empty;
            }
        }
        
        string IWebSocketConnectionInfo.Host => Context.Request.Host.Value;
        string IWebSocketConnectionInfo.Path => Context.Request.Path.Value;
        string IWebSocketConnectionInfo.ClientIpAddress => Context.Request.RemoteIpAddress;
        int IWebSocketConnectionInfo.ClientPort => Context.Request.RemotePort ?? 0;
        IDictionary<string, string> IWebSocketConnectionInfo.Cookies => Context.Request.Cookies.ToDictionary(k => k.Key, v => v.Value);
        IDictionary<string, string> IWebSocketConnectionInfo.Headers => Context.Request.Headers.SelectMany(p => p.Value.Select(v => new { p.Key, Value = v })).ToDictionary(k => k.Key, v => v.Value);

        Guid IWebSocketConnectionInfo.Id { get; } = Guid.NewGuid();
        string IWebSocketConnectionInfo.NegotiatedSubProtocol { get; } = string.Empty;

        #endregion

        private class OwinWebSocketContext : IOwinWebSocketContext
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
}