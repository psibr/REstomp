using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REstomp.Middleware
{
    using StompCommand = StompParser.Command;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// STOMP middleware that ensures the stomp.terminateConnection key is set when an ERROR frame exists.
    /// </summary>
    public class TerminationMiddleware
    {
        public TerminationMiddleware()
        {

        }

        public AppFunc Invoke(AppFunc next) =>
            async (IDictionary<string, object> environment) =>
            {
                await next(environment);

                if((environment["stomp.responseMethod"] as string) == StompCommand.ERROR)
                {
                    environment["stomp.terminateConnection"] = true;
                }                
            };
    }
}