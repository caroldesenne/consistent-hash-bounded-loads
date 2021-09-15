using System.Collections.Generic;

namespace ConsistentHashBoundedLoads
{
    internal class Program
    {
        public static void Main(string[] args)
        {
        }

        public static void RunConsistentHash()
        {
            List<Server> servers = new List<Server>();
            for (int i = 10; i < 25; i++)
                servers.Add(new Server($"{i}", 100));

            ConsistentHashBoundedLoads CHBL = new ConsistentHashBoundedLoads(servers.ToArray());
            
            // look for examples:
            // index=janus DetailedLogs api_cache_status!="NOAPICACHE"
            // host pathAndQuery
            
        }
    }
}