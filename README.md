Owin.WebSocket
==============

An library for handling OWIN WebSocket connections

[![Build status](https://ci.appveyor.com/api/projects/status/muxueaiirqenc859)](https://ci.appveyor.com/project/bryceg/owin-websocket)

This library was born from a need to replace the SignalR default of json serialization.  Some code is borrowed from SignalR and built upon to handle the web socket connection, and allow the need to handle the serialization seperately.  By default most people will use JSON due to the wide understanding of it and native support in the browsers.  Other types could easily be swapped in such as Protobuf or msgpack as you see fit. 


Getting Started:

Install the Nuget package: https://www.nuget.org/packages/Owin.WebSocket/

<b>1) Inherit from WebSocketConnection</b>
```c#
using Owin.WebSocket;

public class MyWebSocket : WebSocketConnection
{
    public override void OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
    {
       //Handle the message from the client
       
       //Example of JSON serialization with the client
       //var json = Encoding.UT8.GetString(message.Array, message.Offset, message.Count);
       //Use something like Json.Net to read the json
    }
    
    //public override void OnOpen(){}
    //public override void OnClose(WebSocketCloseStatus closeStatus, string closeDescription){}
    //public override bool Authenticate(IOwinContext requestContext){return true;}
}
```

<b>2) Map the route for the web socket connection</b>
<br>In your OWIN Startup, set the URI mapping for the websocket connection.  A new instance of this class will be instantiated per web socket connection.  Refer To dependency injection at the bottom of this file if you wish to change the default resolver and use your own IoC.
```c#
using Owin.WebSocket.Extensions;

//For static routes http://foo.com/ws use MapWebSocketRoute and attribute the WebSocketConnection with [WebSocketRoute('/ws')]
app.MapWebSocketRoute<MyWebSocket>();

//For static routes http://foo.com/ws use MapWebSocketRoute
app.MapWebSocketRoute<MyWebSocket>("/ws");

//For dynamic routes where you may want to capture the URI arguments use a Regex route
app.MapWebSocketPattern<MyWebSocket>("/captures/(?<capture1>.+)/(?<capture2>.+)");
``` 

<b>3) Sending data to the client</b>
<br>To send something to the client the WebSocketConnection exposes 3 methods.  
<br>.SendText() for sending text to the client using the text message type flag.
<br>.SendBinary() for sending binary to the client using the binary messge type flag.
<br>.Send() allows you specify the message type for the data you are sending
```c#
using Owin.WebSocket;

public class MyWebSocket : WebSocketConnection
{
    public override void OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
    {
       //Handle the message from the client
       
       //Example of JSON serialization with the client
       var json = Encoding.UTF8.GetString(message.Array, message.Offset, message.Count);

        var toSend = Encoding.UTF8.GetBytes(json);
        
        //Echo the message back to the client as text
        SendText(toSend, true);
    }
}
```

<h5>Javascript Client:</h5>
 http://msdn.microsoft.com/en-us/library/ie/hh673567(v=vs.85).aspx

#####Dependency Injection for WebSocketConnection instance:

The Microsoft Common Service Locator pattern is used for dependency injection.  To use your own IoC pass it along to the MapWebSocketRoute call
```c#
//Autofac example
app.MapWebSocketRoute<MyWebSocket>("/ws", new AutofacServiceLocator(container));
```

