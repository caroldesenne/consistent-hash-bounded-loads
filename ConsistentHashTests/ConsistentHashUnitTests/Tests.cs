using LoadBalancerTests;
using Xunit;

namespace ConsistentHashUnitTests
{
    public class Tests
    {
        [Fact]
        public void TestSkipsLoadedServer()
        {
            var servers = new Server[]
            {
                new ("1", 100),
                new ("2", 100),
                new ("3", 100),
                new ("4", 125)
            };
            var ch = new ConsistentHash(servers);
            ch.InflightRequests = 425;
            Assert.Equal("4", ch.Next("blalba", "bloblo").id);
            Assert.Equal(100, ch.NextBoundedTryNext("blalba", "bloblo").inflightRequest);
            Assert.Equal(100, ch.NextBoundedTryHash("blalba", "bloblo").inflightRequest);
        }
    }
}