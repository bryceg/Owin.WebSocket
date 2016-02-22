using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin.WebSocket.Extensions;

namespace UnitTests.Fleck
{
    [TestClass]
    public class OwinFleckTests : FleckTestsBase
    {
        [TestMethod]
        public void FeatureParityTest()
        {
            using (WebApp.Start(new StartOptions("http://localhost:8989"), startup =>
            {
                startup.MapOwinFleckRoute(ConfigureIntegrationTestConnection);
            }))
            {
                SendIntegrationTestMessages();
                AssertIntegrationTestMessages();
            }
        }

        [TestMethod]
        public void AuthorizationTest()
        {
            using (WebApp.Start(new StartOptions("http://localhost:8989"), startup =>
            {
                startup.MapOwinFleckRoute("/fleck", connection =>
                {
                    var id = ConfigureIntegrationTestConnectionAndGetId(connection);
                    
                    connection.OnAuthenticateRequest = () =>
                    {
                        var result = id % 2 == 1;
                        Send(id, $"Auth {id}: {result}");
                        return result;
                    };

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

                client3.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).Wait();
                client3.Dispose();
                Task.Delay(100).Wait();

                client1.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).Wait();
                client1.Dispose();
                Task.Delay(100).Wait();
            }

            var messages = DequeueMessages();
            Assert.AreEqual(8, messages.Count);
            Assert.AreEqual("Auth 1: True", messages[0]);
            Assert.AreEqual("Open: 1", messages[1]);
            Assert.AreEqual("Auth 2: False", messages[2]);
            Assert.AreEqual("Auth 3: True", messages[3]);
            Assert.AreEqual("Open: 3", messages[4]);
            Assert.AreEqual("User 1: Hello world", messages[5]);
            Assert.AreEqual("Close: 3", messages[6]);
            Assert.AreEqual("Close: 1", messages[7]);
        }
    }
}