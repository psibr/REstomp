using System;
using System.Collections.Generic;
using System.Net;
using REstomp.Client;
using REstomp.Middleware;

namespace REstomp.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5467);
            var parser = new StompParser();

            using(var reStompService = new StompService(endPoint, parser))
            {
                reStompService.Start((middlewareStack) =>
                {
                    middlewareStack.Push(new TerminationMiddleware().Invoke);
                    middlewareStack.Push(new ProtocolVersionMiddleware().Invoke);
                    middlewareStack.Push(new SessionMiddleware().Invoke);
                    middlewareStack.Push(new SendMiddleware().Invoke);
                });

                Console.WriteLine("Service started.");

                var client = new StompClient();

                var session = client.Connect("127.0.0.1", 5467).Result;

                if(session != null)
                {
                    client.Send(new Dictionary<string,string> { ["receipt-id"] = "111" }, "WOO!").Wait();
                }

                Console.ReadLine();
            }
        }
    }
}