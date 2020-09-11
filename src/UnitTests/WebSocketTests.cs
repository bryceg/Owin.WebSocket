using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using CommonServiceLocator;
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

            WebApp.Start(new StartOptions("http://localhost:8989"), startup =>
            {
                startup.MapWebSocketRoute<TestConnection>();
                startup.MapWebSocketRoute<TestConnection>("/ws", sResolver);
                startup.MapWebSocketPattern<TestConnection>("/captures/(?<capture1>.+)/(?<capture2>.+)", sResolver);
            });
        }

        ClientWebSocket StartStaticRouteClient(string route = "/ws")
        {
            var client = new ClientWebSocket();
            client.ConnectAsync(new Uri("ws://localhost:8989" + route), CancellationToken.None).Wait();
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
        public void ConnectionTest_Attribute()
        {
            var client = StartStaticRouteClient("/wsa");
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

            //Let the networking happen
            Thread.Sleep(500);

            client.State.Should().Be(WebSocketState.Closed);
            socket.OnCloseCalled.Should().BeTrue();
            socket.OnCloseAsyncCalled.Should().BeTrue();
        }

        [TestMethod]
        public void CloseWithStatusTest()
        {
            var socket = new TestConnection();
            sResolver.Types[typeof(TestConnection)] = socket;

            var client = StartStaticRouteClient();
            client.State.Should().Be(WebSocketState.Open);

            const string CLOSE_DESCRIPTION = "My Description";

            client.CloseAsync(WebSocketCloseStatus.NormalClosure, CLOSE_DESCRIPTION, CancellationToken.None)
                .Wait();

            //Let the networking happen
            Thread.Sleep(500);

            socket.OnCloseCalled.Should().BeTrue();
            socket.CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
            socket.CloseDescription.Should().Be(CLOSE_DESCRIPTION);

            socket.OnCloseAsyncCalled.Should().BeTrue();
            socket.AsyncCloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
            socket.AsyncCloseDescription.Should().Be(CLOSE_DESCRIPTION);
        
        }

        [TestMethod]
        public void CloseByDisconnectingTest()
        {
            var socket = new TestConnection();
            sResolver.Types[typeof(TestConnection)] = socket;

            var client = StartStaticRouteClient();
            client.State.Should().Be(WebSocketState.Open);

            client.Dispose();
            var task = Task.Run(
                () =>
                    {
                        while (!socket.OnCloseCalled || !socket.OnCloseAsyncCalled)
                            Thread.Sleep(10);
                    });

            task.Wait(TimeSpan.FromMinutes(2)).Should().BeTrue();

            socket.OnCloseCalled.Should().BeTrue();
            socket.OnCloseAsyncCalled.Should().BeTrue();
            //socket.CloseStatus.Should().Be(WebSocketCloseStatus.Empty);
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
        public void ReceiveTest()
        {
            var socket = new TestConnection();
            sResolver.Types[typeof(TestConnection)] = socket;
            var client = StartStaticRouteClient();

            var buffer = new byte[64*1024];
            var segment = new ArraySegment<byte>(buffer);
            var receiveCount = 0;
            var receiveTask = Task.Run(async () =>
            {
                var result = await client.ReceiveAsync(segment, CancellationToken.None);
                receiveCount = result.Count;
            });

            var toSend = "Test Data String";
            SendText(client, toSend).Wait();

            receiveTask.Wait();

            var received = Encoding.UTF8.GetString(segment.Array, segment.Offset, receiveCount);
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

        [TestMethod]
        public void BadRequestTest()
        {
            var client = new WebClient();
            var t = new Action(() => client.OpenRead("http://localhost:8989/ws"));
            var ex = t.ShouldThrow<WebException>().Which;
            (((HttpWebResponse) (ex.Response)).StatusCode).Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public void SendTextTest()
        {
            var socket = new TestConnection();
            sResolver.Types[typeof(TestConnection)] = socket;
            var client = StartStaticRouteClient();

            var text = Encoding.UTF8.GetBytes("My Text to send");
            client.SendAsync(new ArraySegment<byte>(text), WebSocketMessageType.Text, true, CancellationToken.None)
                .Wait();

            Thread.Sleep(50);

            var val = Encoding.UTF8.GetString(socket.LastMessage.Array, socket.LastMessage.Offset, socket.LastMessage.Count);
            val.Should().Be("My Text to send");
            socket.LastMessageType.Should().Be(WebSocketMessageType.Text);
        }

        [TestMethod]
        public void SendBinaryTest()
        {
            var socket = new TestConnection();
            sResolver.Types[typeof(TestConnection)] = socket;
            var client = StartStaticRouteClient();

            var data = new byte[1024];
            data[9] = 4;
            data[599] = 123;
            client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None)
                .Wait();

            Thread.Sleep(50);

            socket.LastMessage.Count.Should().Be(data.Length);
            socket.LastMessageType.Should().Be(WebSocketMessageType.Binary);

            for (var i = 0; i < socket.LastMessage.Count; i++)
            {
                (socket.LastMessage.Array[socket.LastMessage.Offset + i] == data[i]).Should().BeTrue();
            }
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

    [WebSocketRoute("/wsa")]
    class TestConnection: WebSocketConnection
    {
        public ArraySegment<byte> LastMessage { get; set; }
        public WebSocketMessageType LastMessageType { get; set; }
        public bool OnOpenCalled { get; set; }
        public bool OnOpenAsyncCalled { get; set; }
        public bool OnCloseCalled { get; set; }
        public bool OnCloseAsyncCalled { get; set; }
        public IOwinRequest Request { get; set; }

        public WebSocketCloseStatus? CloseStatus { get; set; }
        public string CloseDescription { get; set; }

        public WebSocketCloseStatus? AsyncCloseStatus { get; set; }
        public string AsyncCloseDescription { get; set; }

        public override async Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            LastMessage = message;
            LastMessageType = type;

            //Echo it back
            await Send(message, true, type);
        }

        public override void OnOpen()
        {
            OnOpenCalled = true;
        }

        public override Task OnOpenAsync()
        {
            OnOpenAsyncCalled = true;
            return Task.Delay(0);
        }
        
        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            OnCloseCalled = true;
            CloseStatus = closeStatus;
            CloseDescription = closeStatusDescription;
        }

        public override Task OnCloseAsync(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            OnCloseAsyncCalled = true;
            AsyncCloseStatus = closeStatus;
            AsyncCloseDescription = closeStatusDescription;
            return Task.Delay(0);
        }
    
    }
}
