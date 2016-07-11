using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace REstomp.Middleware
{
    using StompCommand = StompParser.Command;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class SessionMiddleware
    {
        public SessionMiddleware()
        {

        }

        public AppFunc Invoke(AppFunc next) =>
            async (IDictionary<string, object> environment) =>
            {
                var stompCommand = (string)environment["stomp.requestMethod"];

                if(StompCommand.IsConnectRequest(stompCommand))
                {
                    //Generate CONNECTED frame.
                    var frame = new StompFrame(StompCommand.CONNECTED, new Dictionary<string, string>{
                        { "version", "1.2" }
                    }).WriteToEnvironmentResponse(environment);
                }
                else
                {
                    var headers = (ImmutableArray<KeyValuePair<string, string>>)environment["stomp.requestHeaders"];

                    string sessionId;

                    if(headers.TryGetValue("session", out sessionId) && !string.IsNullOrWhiteSpace(sessionId))
                    {
                        //Let pass.
                        await next(environment);
                    }
                    else
                    {
                        //Generate ERROR frame.
                        var frame = new StompFrame(StompCommand.ERROR, new Dictionary<string, string>{
                            { "message", "Session not established." }
                        }).WriteToEnvironmentResponse(environment); 
                    }
                }
            };
    }
}