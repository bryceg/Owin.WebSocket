using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using Microsoft.Owin;
using Owin.WebSocket.Models;

namespace Owin.WebSocket
{
    public class FleckWebSocketConnection : WebSocketConnection, IOwinWebSocketConnection
    {
        private const string PingPongError = "Owin handles ping pong messages internally";

        private readonly OwinWebSocketContext _context;
        private readonly IOwinWebSocketConnection _connection;
        private readonly IWebSocketConnectionInfo _connectionInfo;

        private bool _isAvailable;
        
        public FleckWebSocketConnection()
            : this(1024*64)
        {
        }

        public FleckWebSocketConnection(int maxMessageSize)
            : base(maxMessageSize)
        {
            _connection = this;
            _context = new OwinWebSocketContext(_connection.Context, MaxMessageSize);
            _connectionInfo = new FleckWebSocketConnectionInfo(_connection.Context);
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

        IWebSocketConnectionInfo IWebSocketConnection.ConnectionInfo => _connectionInfo;
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
    }
}