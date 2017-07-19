using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fleck;
using Microsoft.Owin;
using Microsoft.Practices.ServiceLocation;

namespace Owin.WebSocket
{
    public class FleckWebSocketConnectionMiddleware<T> : OwinMiddleware
        where T : FleckWebSocketConnection, new()
    {
        private readonly Action<IWebSocketConnection> _config;
        private readonly Action<IOwinWebSocketConnection> _owinConfig;
        private readonly IServiceLocator _locator;
        private readonly Regex _matchPattern;
        
        public FleckWebSocketConnectionMiddleware(OwinMiddleware next, Action<IWebSocketConnection> config, IServiceLocator locator, Regex matchPattern)
            : this(next, locator, matchPattern)
        {
            _config = config;
        }

        public FleckWebSocketConnectionMiddleware(OwinMiddleware next, Action<IOwinWebSocketConnection> config, IServiceLocator locator, Regex matchPattern)
            : this(next, locator, matchPattern)
        {
            _owinConfig = config;
        }
        
        private FleckWebSocketConnectionMiddleware(OwinMiddleware next, IServiceLocator locator, Regex matchPattern)
            : base(next)
        {
            _locator = locator;
            _matchPattern = matchPattern;
        }

        public override Task Invoke(IOwinContext context)
        {
            var matches = new Dictionary<string, string>();

            if (_matchPattern != null)
            {
                var match = _matchPattern.Match(context.Request.Path.Value);
                if (!match.Success)
                    return Next.Invoke(context);

                for (var i = 1; i <= match.Groups.Count; i++)
                {
                    var name = _matchPattern.GroupNameFromNumber(i);
                    var value = match.Groups[i];
                    matches.Add(name, value.Value);
                }
            }

            var connection = _locator?.GetInstance<T>() ?? new T();

            _config?.Invoke(connection);
            _owinConfig?.Invoke(connection);

            return connection.AcceptSocketAsync(context, matches);
        }
    }
}