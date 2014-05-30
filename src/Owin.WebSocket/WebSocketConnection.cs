using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin.WebSocket.Extensions;

namespace Owin.WebSocket
{
    public abstract class WebSocketConnection
    {
        private readonly TaskQueue mSendQueue;
        private readonly CancellationTokenSource mCancellToken;
        private System.Net.WebSockets.WebSocket mWebSocket;

        public int MaxMessageSize { get; private set; }
        public Dictionary<string, string> Arguments { get; private set; }

        public long SendQueueDepth
        {
            get {  return mSendQueue.Size; }
        }

        protected WebSocketConnection(int maxMessageSize = 1024*64)
        {
            mSendQueue = new TaskQueue();
            mCancellToken = new CancellationTokenSource();
            MaxMessageSize = maxMessageSize;
        }
        
        /// <summary>
        /// Closes the websocket connection
        /// </summary>
        /// <returns></returns>
        public Task Close(WebSocketCloseStatus status, string reason)
        {
            return mWebSocket.CloseAsync(status, reason, CancellationToken.None);
        }

        public void SendAsyncBinary(byte[] buffer, bool endOfMessage)
        {
            SendAsync(new ArraySegment<byte>(buffer), endOfMessage, WebSocketMessageType.Binary);
        }

        public void SendAsyncText(byte[] buffer, bool endOfMessage)
        {
            SendAsync(new ArraySegment<byte>(buffer), endOfMessage, WebSocketMessageType.Text);
        }

        public Task SendAsync(ArraySegment<byte> buffer, bool endOfMessage, WebSocketMessageType type)
        {
            var sendContext = new SendContext { Buffer = buffer, EndOfMessage = endOfMessage, Type = type };

            return mSendQueue.Enqueue(
                async s =>
                    {
                        await mWebSocket.SendAsync(s.Buffer, s.Type, s.EndOfMessage, CancellationToken.None);
                    },
                sendContext);
        }

        public virtual void OnOpen()
        {
        }

        public virtual void OnMessageReceived(ArraySegment<byte> message)
        {
        }

        public virtual void OnClose()
        {
        }

        /// <summary>
        /// Receive one entire message from the web socket
        /// </summary>
        protected async Task<Tuple<ArraySegment<byte>, WebSocketMessageType>> ReceiveOneMessage(byte[] buffer)
        {
            var count = 0;
            WebSocketReceiveResult result;
            do
            {
                var segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                result = await mWebSocket.ReceiveAsync(segment, mCancellToken.Token);

                count += result.Count;
            }
            while (!result.EndOfMessage);

            return new Tuple<ArraySegment<byte>, WebSocketMessageType>(new ArraySegment<byte>(buffer, 0, count), result.MessageType);
        }

        public void AcceptSocket(IOwinContext context, IDictionary<string, string> argumentMatches)
        {
            var accept = context.Get<Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>>("websocket.Accept");
            if (accept == null)
            {
                throw new InvalidOperationException("Not a web socket request");
            }

            Arguments = new Dictionary<string, string>(argumentMatches);

            accept(null, RunWebSocket);
        }

        private async Task RunWebSocket(IDictionary<string, object> websocketContext)
        {
            // Try to get the websocket context from the environment
            object value;
            if (!websocketContext.TryGetValue(typeof(WebSocketContext).FullName, out value))
            {
                throw new InvalidOperationException("Unable to find web socket context");
            }

            mWebSocket = ((WebSocketContext)value).WebSocket;

            OnOpen();

            var buffer = new byte[MaxMessageSize];
            do
            {
                var received = await ReceiveOneMessage(buffer);
                if (received.Item1.Count > 0)
                    OnMessageReceived(received.Item1);
            }
            while (mWebSocket.CloseStatus.GetValueOrDefault(WebSocketCloseStatus.Empty) == WebSocketCloseStatus.Empty);

            await mWebSocket.CloseAsync(mWebSocket.CloseStatus.GetValueOrDefault(WebSocketCloseStatus.Empty),
                mWebSocket.CloseStatusDescription, mCancellToken.Token);

            mCancellToken.Cancel();

            OnClose();
        }
    }

    internal class SendContext
    {
        public ArraySegment<byte> Buffer;
        public bool EndOfMessage;
        public WebSocketMessageType Type;
    }
}