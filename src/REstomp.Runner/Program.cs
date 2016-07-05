using System;
using System.Net;

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
                reStompService.Start();

                Console.WriteLine("Service started.");

                Console.ReadLine();
            }
        }
    }
}
