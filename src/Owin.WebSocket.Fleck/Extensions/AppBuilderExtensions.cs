using System;
using System.Text.RegularExpressions;
using Fleck;
using Microsoft.Practices.ServiceLocation;

namespace Owin.WebSocket.Extensions
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder MapFleckRoute(this IAppBuilder app, Action<IWebSocketConnection> config, IServiceLocator locator = null, string regexPatternMatch = null)
        {
            return app.MapFleckRoute(string.Empty, config, locator, regexPatternMatch);
        }

        public static IAppBuilder MapFleckRoute(this IAppBuilder app, string route, Action<IWebSocketConnection> config, IServiceLocator locator = null, string regexPatternMatch = null)
        {
            return app.MapFleckRoute<FleckWebSocketConnection>(route, config, locator, regexPatternMatch);
        }

        public static IAppBuilder MapFleckRoute<T>(this IAppBuilder app, Action<IWebSocketConnection> config, IServiceLocator locator = null, string regexPatternMatch = null)
            where T : FleckWebSocketConnection, new()
        {
            return app.MapFleckRoute<T>(string.Empty, config, locator, regexPatternMatch);
        }

        public static IAppBuilder MapFleckRoute<T>(this IAppBuilder app, string route, Action<IWebSocketConnection> config, IServiceLocator locator = null, string regexPatternMatch = null)
            where T : FleckWebSocketConnection, new()
        {
            var regex = string.IsNullOrWhiteSpace(regexPatternMatch) ? null : new Regex(regexPatternMatch, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return app.Map(route, a => a.Use<FleckWebSocketConnectionMiddleware<T>>(config, locator, regex));
        }
        
        public static IAppBuilder MapOwinFleckRoute(this IAppBuilder app, Action<IOwinWebSocketConnection> config, IServiceLocator locator = null, string regexPatternMatch = null)
        {
            return app.MapOwinFleckRoute(string.Empty, config, locator, regexPatternMatch);
        }

        public static IAppBuilder MapOwinFleckRoute(this IAppBuilder app, string route, Action<IOwinWebSocketConnection> config, IServiceLocator locator = null, string regexPatternMatch = null)
        {
            return app.MapOwinFleckRoute<FleckWebSocketConnection>(route, config, locator, regexPatternMatch);
        }

        public static IAppBuilder MapOwinFleckRoute<T>(this IAppBuilder app, Action<IOwinWebSocketConnection> config, IServiceLocator locator = null, string regexPatternMatch = null)
            where T : FleckWebSocketConnection, new()
        {
            return app.MapOwinFleckRoute<T>(string.Empty, config, locator, regexPatternMatch);
        }

        public static IAppBuilder MapOwinFleckRoute<T>(this IAppBuilder app, string route, Action<IOwinWebSocketConnection> config, IServiceLocator locator = null, string regexPatternMatch = null)
            where T : FleckWebSocketConnection, new()
        {
            var regex = string.IsNullOrWhiteSpace(regexPatternMatch) ? null : new Regex(regexPatternMatch, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return app.Map(route, a => a.Use<FleckWebSocketConnectionMiddleware<T>>(config, locator, regex));
        }
    }
}