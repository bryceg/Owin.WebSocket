using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Fleck;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin.WebSocket.Extensions;

namespace UnitTests
{
    [TestClass]
    public class FleckTests
    {
        private readonly ConcurrentDictionary<int, IWebSocketConnection> _connections = new ConcurrentDictionary<int, IWebSocketConnection>();
        private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();

        private void Write(int id, string message)
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

        [TestMethod]
        public void IntegrationTest()
        {
            using (WebApp.Start(new StartOptions("http://localhost:8989"), startup =>
            {
                var idSeed = 0;

                startup.MapWebSocketRoute("/fleck", connection =>
                {
                    var id = Interlocked.Increment(ref idSeed);

                    connection.OnOpen = () => Write(id, $"Open: {id}");
                    connection.OnClose = () => Write(id, $"Close: {id}");
                    connection.OnError = ex => Write(-1, $"Error: {id} - {ex.Message}");
                    connection.OnMessage = m => Write(id, $"User {id}: {m}");
                    connection.OnAuthenticateRequest = r =>
                    {
                        var result = id % 2 == 1;
                        Write(id, $"Auth {id}: {result}");
                        return result;
                    };

                    _connections.TryAdd(id, connection);
                });
            }))
            using (var client1 = new ClientWebSocket())
            using (var client2 = new ClientWebSocket())
            using (var client3 = new ClientWebSocket())
            {
                client1.ConnectAsync(new Uri("ws://localhost:8989/fleck"), CancellationToken.None).Wait();
                
                try
                {
                    client2.ConnectAsync(new Uri("ws://localhost:8989/fleck"), CancellationToken.None).Wait();
                    Assert.Fail("Client 2 should not be unauthorized");
                }
                catch (AggregateException ex)
                {
                    Assert.AreEqual("Unable to connect to the remote server", ex.InnerException.Message);
                }
                
                client3.ConnectAsync(new Uri("ws://localhost:8989/fleck"), CancellationToken.None).Wait();

                var bytes3 = new byte[1024];
                var segment3 = new ArraySegment<byte>(bytes3);
                var receive3 = client3.ReceiveAsync(segment3, CancellationToken.None);

                var message1 = "Hello world";
                var bytes1 = Encoding.UTF8.GetBytes(message1);
                var segment1 = new ArraySegment<byte>(bytes1);
                client1.SendAsync(segment1, WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                var result3 = receive3.Result;
                Assert.AreEqual(WebSocketMessageType.Text, result3.MessageType);
                var message3 = Encoding.UTF8.GetString(segment3.Array, 0, result3.Count);
                Assert.AreEqual(message3, "User 1: Hello world");


                client1.Abort();
                client3.Abort();
            }

            var messages = _messages.ToArray();
            Assert.AreEqual(8, messages.Length);
            Assert.AreEqual("Auth 1: True", messages[0]);
            Assert.AreEqual("Open: 1", messages[1]);
            Assert.AreEqual("Auth 2: False", messages[2]);
            Assert.AreEqual("Auth 3: True", messages[3]);
            Assert.AreEqual("Open: 3", messages[4]);
            Assert.AreEqual("User 1: Hello world", messages[5]);
            Assert.IsTrue(messages[6].StartsWith("Error: 1"));
            Assert.IsTrue(messages[7].StartsWith("Error: 3"));
        }
    }
}
