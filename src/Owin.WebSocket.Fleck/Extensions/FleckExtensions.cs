using System;
using System.Text.RegularExpressions;

namespace Owin.WebSocket.Extensions
{
    public static class FleckExtensions
    {
        public static IAppBuilder MapWebSocketRoute(this IAppBuilder app, Action<IOwinWebSocketConnection> config)
        {
            return app.MapWebSocketRoute(string.Empty, config);
        }

        public static IAppBuilder MapWebSocketRoute(this IAppBuilder app, string route, Action<IOwinWebSocketConnection> config)
        {
            return app.Map(route, a => a.Use<FleckWebSocketConnectionMiddleware>(config));
        }

        public static IAppBuilder MapWebSocketPattern(this IAppBuilder app, string regexPatternMatch, Action<IOwinWebSocketConnection> config)
        {
            return app.Use<FleckWebSocketConnectionMiddleware>(config, new Regex(regexPatternMatch, RegexOptions.Compiled | RegexOptions.IgnoreCase));
        }
    }
}