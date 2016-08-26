using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REstomp
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using MidFunc = Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>;

    public sealed class StompPipeline
    {
        private AppFunc Application;

        public StompPipeline(Stack<MidFunc> builder)
        {
            AppFunc currentMiddleware = (IDictionary<string, object> environment) 
                => Task.CompletedTask;

            //Here we pop the stack and pass the previous middleware.
            while (builder.Count > 0)
            {
                currentMiddleware = builder.Pop().Invoke(currentMiddleware);
            }

            Application = currentMiddleware;
        }

        public async Task<StompFrame> Process(IDictionary<string, object> environment)
        {
            await Application.Invoke(environment);

            return environment.ReadFromEnvironmentResponse();
        }
    }
}