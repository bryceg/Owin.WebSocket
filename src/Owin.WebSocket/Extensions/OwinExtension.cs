using System.Text.RegularExpressions;

namespace Owin.WebSocket.Extensions
{
    public static class OwinExtension
    {
        /// <summary>
        /// Maps a static uri to a web socket consumer
        /// </summary>
        /// <typeparam name="T">Type of WebSocketHubConsumer</typeparam>
        /// <param name="app">Owin App</param>
        /// <param name="route">Static URI to map to the hub</param>
        public static void MapWebSocketRoute<T>(this IAppBuilder app, string route) 
            where T : WebSocketConnection
        {
            app.Map(route, config => config.Use<WebSocketConnectionMiddleware<T>>());
        }

        /// <summary>
        /// Maps a uri pattern to a web socket consumer
        /// </summary>
        /// <typeparam name="T">Type of WebSocketHubConsumer</typeparam>
        /// <param name="app">Owin app</param>
        /// <param name="regexPatternMatch">Regex pattern of the URI to match.  Capture groups will be sent to the hub on the Arguments property</param>
        public static void MapWebSocketPattern<T>(this IAppBuilder app, string regexPatternMatch)
            where T : WebSocketConnection
        {
            app.Use<WebSocketConnectionMiddleware<T>>(new Regex(regexPatternMatch, RegexOptions.Compiled | RegexOptions.IgnoreCase));
        }
    }
}
