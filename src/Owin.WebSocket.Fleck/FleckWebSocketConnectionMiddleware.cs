using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fleck;
using Microsoft.Owin;

namespace Owin.WebSocket
{
    public class FleckWebSocketConnectionMiddleware : OwinMiddleware
    {
        private readonly Regex _matchPattern;

        private readonly Action<IWebSocketConnection> _config;

        private readonly Action<IOwinWebSocketConnection> _owinConfig;

        public FleckWebSocketConnectionMiddleware(OwinMiddleware next, Action<IWebSocketConnection> config)
            : base(next)
        {
            _config = config;
        }

        public FleckWebSocketConnectionMiddleware(OwinMiddleware next, Action<IWebSocketConnection> config, Regex matchPattern)
            : this(next, config)
        {
            _matchPattern = matchPattern;
        }

        public FleckWebSocketConnectionMiddleware(OwinMiddleware next, Action<IOwinWebSocketConnection> config)
            : base(next)
        {
            _owinConfig = config;
        }

        public FleckWebSocketConnectionMiddleware(OwinMiddleware next, Action<IOwinWebSocketConnection> config, Regex matchPattern)
            : this(next, config)
        {
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

            var connection = new FleckWebSocketConnection();

            _config?.Invoke(connection);
            _owinConfig?.Invoke(connection);

            return connection.AcceptSocketAsync(context, matches);
        }
    }
}