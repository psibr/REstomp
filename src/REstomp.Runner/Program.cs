using System;
using System.Net;
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
                    middlewareStack.Push(new ProtocolVersionMiddleware().Invoke);
                    middlewareStack.Push(new SessionMiddleware(new SessionOptions
                    {
                        AcceptedVersions = new [] { "1.1", "1.2" }
                    }).Invoke);
                });

                Console.WriteLine("Service started.");

                Console.ReadLine();
            }
        }
    }
}