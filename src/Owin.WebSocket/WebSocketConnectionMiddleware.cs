using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Owin.WebSocket
{
    public class WebSocketConnectionMiddleware<T> : OwinMiddleware where T : WebSocketConnection
    {
        private readonly Regex mMatchPattern;

        public WebSocketConnectionMiddleware(OwinMiddleware next):base(next)
        {
        }

        public WebSocketConnectionMiddleware(OwinMiddleware next, Regex matchPattern)
            : base(next)
        {
            mMatchPattern = matchPattern;
        }

        public override Task Invoke(IOwinContext context)
        {
            var matches = new Dictionary<string, string>();

            if (mMatchPattern != null)
            {
                var match = mMatchPattern.Match(context.Request.Path.Value);
                if(!match.Success)
                    return Next.Invoke(context);

                for (var i = 1; i <= match.Groups.Count; i++)
                {
                    var name = mMatchPattern.GroupNameFromNumber(i);
                    var value = match.Groups[i];
                    matches.Add(name, value.Value);
                }
            }


            var socketHandler = GlobalContext.DependencyResolver.GetInstance<T>();
            socketHandler.AcceptSocket(context, matches);

            return Task.FromResult<object>(null);
        }
    }
}