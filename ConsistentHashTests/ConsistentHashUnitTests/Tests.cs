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
            var load = 1.1;
            Assert.Equal("4", ConsistentHash.Next(servers, load, "blalba", "bloblo", "vanilla").id);
            Assert.Equal(100, ConsistentHash.Next(servers, load, "blalba", "bloblo", "google").inflightRequest);
            Assert.Equal(100, ConsistentHash.Next(servers, load, "blalba", "bloblo", "jumps").inflightRequest);
        }
        
        [Fact]
        public void TestDoesNotGetLighterServer()
        {
            var servers = new Server[]
            {
                new ("1", 100),
                new ("2", 125),
                new ("3", 125),
                new ("4", 125)
            };
            var load = 1.1;
            Assert.Equal("4", ConsistentHash.Next(servers, load, "blalba", "bloblo", "vanilla").id);
            Assert.NotEqual(100, ConsistentHash.Next(servers, load, "blalba", "bloblo", "google").inflightRequest);
            Assert.NotEqual(100, ConsistentHash.Next(servers, load, "blalba", "bloblo", "jumps").inflightRequest);
        }
        
        [Fact]
        public void TestSkipsSuperLoadedServer()
        {
            var servers = new Server[]
            {
                new ("1", 100),
                new ("2", 125),
                new ("3", 125),
                new ("4", 300)
            };
            var load = 1.1;
            Assert.Equal("4", ConsistentHash.Next(servers, load, "blalba", "bloblo", "vanilla").id);
            Assert.NotEqual(300, ConsistentHash.Next(servers, load, "blalba", "bloblo", "google").inflightRequest);
            Assert.NotEqual(300, ConsistentHash.Next(servers, load, "blalba", "bloblo", "jumps").inflightRequest);
        }
    }
}