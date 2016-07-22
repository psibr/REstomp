using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REstomp.Middleware
{
    using StompCommand = StompParser.Command;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class ProtocolVersionMiddleware
    {
        public ProtocolVersionMiddleware()
        {

        }

        public AppFunc Invoke(AppFunc next) =>
            async (IDictionary<string, object> environment) =>
            {
                var requestFrame = environment.ReadFromEnvironmentRequest();

                string stompProtocolVersion;

                if(StompCommand.IsConnectRequest(requestFrame.Command))
                {                    
                    string stompAcceptVersion;
                    if(requestFrame.Headers.TryGetValue("accept-version", out stompAcceptVersion))
                        stompProtocolVersion = "1.0";
                    else
                        stompProtocolVersion = stompAcceptVersion;

                    if(environment.ContainsKey("stomp.protocolVersion"))
                        environment["stomp.protocolVersion"] = stompProtocolVersion;
                    else
                        environment.Add("stomp.protocolVersion", stompProtocolVersion);
                }                

                await next(environment);
            };
    }
}