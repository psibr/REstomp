using System;
using Xunit;

namespace REstomp.Tests
{
    public class ParserTests
    {
        [Fact]
        public void Test1() 
        {
            using(var reStompService = new REstomp.StompService(System.Net.IPAddress.Parse("127.0.0.1"), 5467))
            {
                reStompService.Start();
            }

            Assert.True(true);
        }
    }
}