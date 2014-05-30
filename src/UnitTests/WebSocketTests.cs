using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.ServiceLocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin.WebSocket;
using Owin.WebSocket.Extensions;
using FluentAssertions;

namespace UnitTests
{
    [TestClass]
    public class WebSocketTests
    {
        private static IDisposable sWeb;
        private static IServiceLocator sResolver;

        [ClassCleanup]
        public static void Cleanup()
        {
            if(sWeb != null)
                sWeb.Dispose();
        }

        [ClassInitialize]
        public static void Init(TestContext test)
        {
            sResolver = new TestResolver();
            GlobalContext.DependencyResolver = sResolver;

            WebApp.Start(new StartOptions("http://localhost:8989"), startup =>
            {
                startup.MapWebSocketRoute<TestConnection>("/ws");
            });
        }

        [TestMethod]
        public void TestMethod1()
        {
            var client = new ClientWebSocket();
            client.ConnectAsync(new Uri("ws://localhost:8989/ws"), CancellationToken.None).Wait();
            client.State.Should().Be(WebSocketState.Open);

            var connection = sResolver.GetInstance<TestConnection>();

            var toSend = "Test Data String";
            SendText(client, toSend).Wait();

            connection.LastMessage.Should().NotBeNull();
            var received = Encoding.UTF8.GetString(
                connection.LastMessage.Array,
                connection.LastMessage.Offset,
                connection.LastMessage.Count);

            received.Should().Be(toSend);

            client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                .Wait();

            client.State.Should().Be(WebSocketState.Closed);
        }

        async Task SendText(ClientWebSocket socket, string data)
        {
            var t = Encoding.UTF8.GetBytes(data);
            await socket.SendAsync(
                new ArraySegment<byte>(t),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

    }

    class TestResolver: IServiceLocator
    {
        public Dictionary<Type, object> mTypes = new Dictionary<Type, object>();

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public object GetInstance(Type serviceType)
        {
            object t;
            if (!mTypes.TryGetValue(serviceType, out t))
            {
                t = Activator.CreateInstance(serviceType);
                mTypes.Add(serviceType, t);
            }

            return t;
        }

        public object GetInstance(Type serviceType, string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public TService GetInstance<TService>()
        {
            object t;
            if (!mTypes.TryGetValue(typeof(TService), out t))
            {
                t = Activator.CreateInstance<TService>();
                mTypes.Add(typeof(TService), t);
            }

            return (TService)t;
        }

        public TService GetInstance<TService>(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TService> GetAllInstances<TService>()
        {
            throw new NotImplementedException();
        }
    }

    class TestConnection: WebSocketConnection
    {
        public ArraySegment<byte> LastMessage { get; set; }

        public override void OnMessageReceived(ArraySegment<byte> message)
        {
            LastMessage = message;
        }
    }
}
