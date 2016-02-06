using System;
using System.Text.RegularExpressions;
using Fleck;

namespace Owin.WebSocket.Extensions
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder MapFleckRoute(this IAppBuilder app, Action<IWebSocketConnection> config)
        {
            return app.MapFleckRoute(string.Empty, config);
        }

        public static IAppBuilder MapFleckRoute(this IAppBuilder app, string route, Action<IWebSocketConnection> config)
        {
            return app.Map(route, a => a.Use<FleckWebSocketConnectionMiddleware>(config));
        }

        public static IAppBuilder MapFleckPattern(this IAppBuilder app, string regexPatternMatch, Action<IWebSocketConnection> config)
        {
            return app.Use<FleckWebSocketConnectionMiddleware>(config, new Regex(regexPatternMatch, RegexOptions.Compiled | RegexOptions.IgnoreCase));
        }

        public static IAppBuilder MapOwinFleckRoute(this IAppBuilder app, Action<IOwinWebSocketConnection> config)
        {
            return app.MapOwinFleckRoute(string.Empty, config);
        }

        public static IAppBuilder MapOwinFleckRoute(this IAppBuilder app, string route, Action<IOwinWebSocketConnection> config)
        {
            return app.Map(route, a => a.Use<FleckWebSocketConnectionMiddleware>(config));
        }

        public static IAppBuilder MapOwinFleckPattern(this IAppBuilder app, string regexPatternMatch, Action<IOwinWebSocketConnection> config)
        {
            return app.Use<FleckWebSocketConnectionMiddleware>(config, new Regex(regexPatternMatch, RegexOptions.Compiled | RegexOptions.IgnoreCase));
        }
    }
}