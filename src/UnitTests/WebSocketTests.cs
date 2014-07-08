using System;
using System.Collections.Generic;
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
        private static TestResolver sResolver;

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
                startup.MapWebSocketPattern<TestConnection>("/captures/(?<capture1>.+)/(?<capture2>.+)");
            });
        }

        ClientWebSocket StartStaticRouteClient()
        {
            var client = new ClientWebSocket();
            client.ConnectAsync(new Uri("ws://localhost:8989/ws"), CancellationToken.None).Wait();
            return client;
        }

        ClientWebSocket StartRegextRouteClient(string param1, string param2)
        {
            var client = new ClientWebSocket();
            client.ConnectAsync(
                new Uri("ws://localhost:8989/captures/" + param1 + "/" + param2), 
                CancellationToken.None)
                .Wait();

            return client;
        }

        [TestMethod]
        public void ConnectionTest()
        {
            var client = StartStaticRouteClient();
            client.State.Should().Be(WebSocketState.Open);

            client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                .Wait();

            client.State.Should().Be(WebSocketState.Closed);
        }

        [TestMethod]
        public void CloseWithEmptyStatusTest()
        {
            var socket = new TestConnection();
            sResolver.Types[typeof(TestConnection)] = socket;

            var client = StartStaticRouteClient();
            client.State.Should().Be(WebSocketState.Open);

            client.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None)
                .Wait();

            client.State.Should().Be(WebSocketState.Closed);
            socket.OnCloseCalled.Should().BeTrue();
        }

        [TestMethod]
        public void SendTest()
        {
            var socket = new TestConnection();
            sResolver.Types[typeof(TestConnection)] = socket;
            var client = StartStaticRouteClient();

            var toSend = "Test Data String";
            SendText(client, toSend).Wait();
            Thread.Sleep(100);

            socket.LastMessage.Should().NotBeNull();
            var received = Encoding.UTF8.GetString(
                socket.LastMessage.Array,
                socket.LastMessage.Offset,
                socket.LastMessage.Count);

            received.Should().Be(toSend);
        }

        [TestMethod]
        public void ArgumentsTest()
        {
            var socket = new TestConnection();
            sResolver.Types[typeof(TestConnection)] = socket;
            var param1 = "foo1";
            var param2 = "foo2";
            var client = StartRegextRouteClient(param1, param2);

            socket.Arguments["capture1"].Should().Be(param1);
            socket.Arguments["capture2"].Should().Be(param2);
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
        public Dictionary<Type, object> Types = new Dictionary<Type, object>();

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public object GetInstance(Type serviceType)
        {
            object t;
            if (!Types.TryGetValue(serviceType, out t))
            {
                t = Activator.CreateInstance(serviceType);
                Types.Add(serviceType, t);
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
            if (!Types.TryGetValue(typeof(TService), out t))
            {
                t = Activator.CreateInstance<TService>();
                Types.Add(typeof(TService), t);
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
        public bool OnOpenCalled { get; set; }
        public bool OnCloseCalled { get; set; }

        public override void OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            LastMessage = message;
        }

        public override void OnOpen()
        {
            OnOpenCalled = true;
        }

        public override void OnClose()
        {
            OnCloseCalled = true;
        }
    }
}
