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
            async environment =>
            {
                var requestFrame = environment.ReadFromEnvironmentRequest();

                if (StompCommand.IsConnectRequest(requestFrame.Command))
                {
                    string stompAcceptVersion;
                    var stompProtocolVersion = requestFrame.Headers.TryGetValue("accept-version", out stompAcceptVersion)
                        ? stompAcceptVersion
                        : "1.0";

                    if (environment.ContainsKey("stomp.protocolVersion"))
                        environment["stomp.protocolVersion"] = stompProtocolVersion;
                    else
                        environment.Add("stomp.protocolVersion", stompProtocolVersion);
                }

                await next(environment);
            };
    }
}