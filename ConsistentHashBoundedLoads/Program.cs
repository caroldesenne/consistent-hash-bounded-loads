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
            var ids = new []{"1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "20", "21", "22", "23"};
            List<Server> servers = new List<Server>();
            foreach(var id in ids)
                servers.Add(new Server(id, 100));

            ConsistentHashBoundedLoads CHBL = new ConsistentHashBoundedLoads(servers.ToArray());
            
            // look for examples:
            // index=janus DetailedLogs api_cache_status!="NOAPICACHE"
            // host pathAndQuery
            
        }
    }
}