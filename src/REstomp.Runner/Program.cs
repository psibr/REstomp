using System;

namespace REstomp.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using(var reStompService = new REstomp.StompService(System.Net.IPAddress.Parse("127.0.0.1"), 5467))
            {
                reStompService.Start();

                Console.WriteLine("Service started.");

                Console.ReadLine();
            }
        }
    }
}
