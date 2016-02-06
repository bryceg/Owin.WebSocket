using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    public abstract class FleckTestsBase
    {
        private readonly ConcurrentDictionary<int, IWebSocketConnection> _connections = new ConcurrentDictionary<int, IWebSocketConnection>();
        private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();

        private int _idSeed;

        protected void AddConnection(int id, IWebSocketConnection connection)
        {
            _connections.TryAdd(id, connection);
        }

        protected void RemoveConnection(int id)
        {
            IWebSocketConnection connection;
            _connections.TryRemove(id, out connection);
        }

        protected void Send(int id, string message)
        {
            _messages.Enqueue(message);

            if (id == -1)
                return;

            foreach (var connectionPair in _connections)
            {
                if (!connectionPair.Value.IsAvailable)
                    continue;

                if (connectionPair.Key == id)
                    continue;

                connectionPair.Value.Send(message).Wait();
            }
        }

        protected IList<string> DequeueMessages()
        {
            var results = new List<string>(_messages.Count);
            string message;
            while (_messages.TryDequeue(out message))
            {
                results.Add(message);
            }

            return results;
        }

        protected void ConfigureIntegrationTestConnection(IWebSocketConnection connection)
        {
            ConfigureIntegrationTestConnectionAndGetId(connection);
        }

        protected int ConfigureIntegrationTestConnectionAndGetId(IWebSocketConnection connection)
        {
            var id = Interlocked.Increment(ref _idSeed);

            connection.OnOpen = () =>
            {
                AddConnection(id, connection);
                Send(id, $"Open: {id}");
            };
            connection.OnClose = () =>
            {
                Send(id, $"Close: {id}");
                RemoveConnection(id);
            };
            connection.OnError = ex =>
            {
                Send(-1, $"Error: {id} - {ex.ToString()}");
            };
            connection.OnMessage = m =>
            {
                Send(id, $"User {id}: {m}");
            };

            return id;
        }

        protected void SendIntegrationTestMessages()
        {
            using (var client1 = new ClientWebSocket())
            using (var client2 = new ClientWebSocket())
            {
                client1.ConnectAsync(new Uri("ws://localhost:8989"), CancellationToken.None).Wait();
                client2.ConnectAsync(new Uri("ws://localhost:8989"), CancellationToken.None).Wait();

                var bytes2 = new byte[1024];
                var segment2 = new ArraySegment<byte>(bytes2);
                var receive2 = client2.ReceiveAsync(segment2, CancellationToken.None);

                var message1 = "Hello world";
                var bytes1 = Encoding.UTF8.GetBytes(message1);
                var segment1 = new ArraySegment<byte>(bytes1);
                client1.SendAsync(segment1, WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                var result2 = receive2.Result;
                Assert.AreEqual(WebSocketMessageType.Text, result2.MessageType);
                var message3 = Encoding.UTF8.GetString(segment2.Array, 0, result2.Count);
                Assert.AreEqual(message3, "User 1: Hello world");

                client2.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).Wait();
                client2.Dispose();
                Task.Delay(100).Wait();

                client1.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).Wait();
                client1.Dispose();
                Task.Delay(100).Wait();
            }
        }

        protected void AssertIntegrationTestMessages()
        {
            var messages = DequeueMessages();
            Assert.AreEqual(5, messages.Count);
            Assert.AreEqual("Open: 1", messages[0]);
            Assert.AreEqual("Open: 2", messages[1]);
            Assert.AreEqual("User 1: Hello world", messages[2]);
            Assert.AreEqual("Close: 2", messages[3]);
            Assert.AreEqual("Close: 1", messages[4]);
        }
    }
}