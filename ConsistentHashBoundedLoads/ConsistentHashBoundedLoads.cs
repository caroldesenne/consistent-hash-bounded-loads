using System.Diagnostics;
using System.Linq;

namespace ConsistentHashBoundedLoads
{
    public class ConsistentHashBoundedLoads
    {
        public readonly Server[] Servers;
        public AtomicCounter TotalInflightRequestCount;
        public double load = 1.25;
        private readonly (uint Hashcode, Server Server)[] _ring;
        
        public ConsistentHashBoundedLoads(Server[] servers)
        {
            Servers = servers;
            _ring =
                servers
                    .OrderBy(x => x.Id)
                    .SelectMany(server =>
                        Enumerable.Range(0, server.Replicas)
                            .Select(i => (Hashcode: MurmurHash2.Hash(server.Id, $"{i}"), server)))
                    .OrderBy(x => x.Hashcode).ToArray();
        }
        
        // TODO: 1. try next available server in the ring (_ring[i++])
        // TODO: 2. try hash(i+j), j being the try

        public Server Next(string host, string pathAndQuery)
        {
            var ring = _ring;
            if (ring.Length == 0)
                return null;
            if (ring.Length == 1)
                return ring[0].Server;

            var hash = MurmurHash2.Hash(host, pathAndQuery);
            return Next(hash);
        }
        
        private Server Next(uint hash)
        {
            var ring = _ring;
            var begin = 0;
            var end = ring.Length - 1;

            if (ring[end].Hashcode < hash || ring[0].Hashcode > hash)
                return ring[0].Server;

            while (end - begin > 1)
            {
                var mid = (end + begin) / 2;
                if (ring[mid].Hashcode >= hash)
                    end = mid;
                else
                    begin = mid;
            }
            Debug.Assert(ring[begin].Hashcode <= hash && ring[end].Hashcode >= hash);

            return ring[end].Server;
        }
    }
}