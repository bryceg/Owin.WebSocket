Owin.WebSocket
==============

An library for handling OWIN WebSocket connections

Getting Started:

1) Inherit from WebSocketConnection

    using Owin.WebSocket;

    public class MyWebSocket : WebSocketConnection
    {
        public override void OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
           //Handle the message from the client
           //json = Encoding.UT8.GetString(message.Array, message.Offset, message.Count);
           //Use something like Json.Net to read the json
        }
    }

2) In your OWIN Startup, set the URI mapping for the websocket connection.

     using Owin.WebSocket.Extensions;

     //For static routes 'http://foo.com/ws' use MapWebSocketRoute
     app.MapWebSocketRoute<MyWebSocket>("/ws");

     //For dynamic routes where you may want to capture the URI arguments use a Regex route
     app.MapWebSocketPattern<MyWebSocket>("/captures/(?<capture1>.+)/(?<capture2>.+)");
