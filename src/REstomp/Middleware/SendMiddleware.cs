using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace REstomp.Middleware
{
    using StompCommand = StompParser.Command;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class SendMiddleware
    {
        protected SendOptions Options { get; }
        public SendMiddleware(SendOptions options = null)
        {
            Options = options ?? new SendOptions(); 
        }

        public AppFunc Invoke(AppFunc next) =>
            async environment =>
            {
                var requestFrame = environment.ReadFromEnvironmentRequest();

                if(requestFrame.Command == StompCommand.SEND)
                {
                    var headers = requestFrame.Headers.UniqueKeys();

                    var receiptId = headers.ContainsKey("receipt-id") ? headers["receipt-id"] : null;

                    var result = Options.SendHandler.Invoke(headers, requestFrame.Body.ToArray());

                    if(result.Success && receiptId != null)
                    {
                        new StompFrame(StompCommand.RECEIPT, new Dictionary<string, string>
                        {
                            ["receipt-id"] = receiptId
                        }).WriteToEnvironmentResponse(environment);
                    }
                    else if(!result.Success)
                    {
                        new StompFrame(StompCommand.ERROR, new Dictionary<string, string>
                        {
                            ["message"] = result.ErrorMessage ?? "SEND frame failed to process for unknown reasons."
                        }).WriteToEnvironmentResponse(environment);  
                    }
                }
                else
                {
                    await next(environment);
                }

            };
    }

    public class SendOptions
    {
        private static List<Tuple<IDictionary<string, string>, byte[]>> MessageStorage { get; set; } =
            new List<Tuple<IDictionary<string, string>, byte[]>>();

        public Func<IDictionary<string, string>, byte[], SendResult> SendHandler { get; set; } =
        (headers, content) => 
        {
            MessageStorage.Add(Tuple.Create(headers, content));

            return new SendResult { Success = true };
        };
    }

    public class SendResult
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }
    }
}