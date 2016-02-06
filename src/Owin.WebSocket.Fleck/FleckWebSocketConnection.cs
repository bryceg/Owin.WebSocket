using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using Microsoft.Owin;

namespace Owin.WebSocket
{
    public sealed class FleckWebSocketConnection : WebSocketConnection, IOwinWebSocketConnection, IWebSocketConnectionInfo
    {
        private readonly IOwinWebSocketConnection _connection;

        private bool _isAvailable;

        public FleckWebSocketConnection()
        {
            _connection = this;
        }

        #region WebSocketConnection

        public override bool AuthenticateRequest(IOwinRequest request)
        {
            return _connection.OnAuthenticateRequest?.Invoke(request) ?? true;
        }

        public override Task<bool> AuthenticateRequestAsync(IOwinRequest request)
        {
            return _connection.OnAuthenticateRequestAsync?.Invoke(request) ?? Task.FromResult(true);
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
            _connection.OnClose?.Invoke();
            _connection.OnCloseOwin?.Invoke(closeStatus, closeStatusDescription);
        }

        public override Task OnCloseAsync(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            return _connection.OnCloseOwinAsync?.Invoke(closeStatus, closeStatusDescription) ?? Task.FromResult(true);
        }

        public override Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            if (type == WebSocketMessageType.Binary && _connection.OnBinary != null)
            {
                var array = message.ToArray();
                _connection.OnBinary.Invoke(array);
            }
            else if (type == WebSocketMessageType.Text && _connection.OnMessage != null)
            {
                var @string = Encoding.UTF8.GetString(message.Array, 0, message.Count);
                _connection.OnMessage.Invoke(@string);
            }

            return Task.FromResult(true);
        }

        public override void OnReceiveError(Exception error)
        {
            _connection.OnError?.Invoke(error);
        }

        #endregion
        
        #region IOwinWebSocketConnection

        Func<Task> IOwinWebSocketConnection.OnOpenAsync { get; set; }
        Action<WebSocketCloseStatus?, string> IOwinWebSocketConnection.OnCloseOwin { get; set; }
        Func<WebSocketCloseStatus?, string, Task> IOwinWebSocketConnection.OnCloseOwinAsync { get; set; }
        Func<IOwinRequest, bool> IOwinWebSocketConnection.OnAuthenticateRequest { get; set; }
        Func<IOwinRequest, Task<bool>> IOwinWebSocketConnection.OnAuthenticateRequestAsync { get; set; }

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
            set { throw new NotSupportedException(); }
        }

        Action<byte[]> IWebSocketConnection.OnPong
        {
            get { return null; }
            set { throw new NotSupportedException(); }
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
            throw new NotSupportedException();
        }

        Task IWebSocketConnection.SendPong(byte[] message)
        {
            throw new NotSupportedException();
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
    }
}