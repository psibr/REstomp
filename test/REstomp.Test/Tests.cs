using System;
using System.Net;
using Xunit;

namespace REstomp.Test
{
    public class ParserTests
    {
        [Fact]
        public void Test1() 
        {
            using(var reStompService = new StompService(IPAddress.Parse("127.0.0.1"), 5467))
            {
                reStompService.Start();
            }

            Assert.True(true);
        }
    }
}