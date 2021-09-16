using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LoadBalancerTests
{
    public class ApiCacheServers
    {
        public Server[] servers { get; set; }
    }

    public class Server
    {
        public string id { get; set; }
        public int inflightRequest { get; set; }
        public int replicas { get; set; }
        public Uri endpoint { get; set; }

        public Server(string id, int inflightRequest)
        {
            this.id = id;
            this.inflightRequest = inflightRequest;
            this.replicas = 1;
        }
    }
    
    public class ConsistentHash
    {
        public readonly Server[] Servers;
        public int InflightRequests = 0;
        public double Load = 1.10;
        private readonly (uint Hashcode, Server Server)[] _ring;

        public ConsistentHash(Server[] servers)
        {
            InflightRequests = 0;
            Servers = servers;
            _ring =
                servers
                    .OrderBy(x => x.id)
                    .SelectMany(server =>
                        Enumerable.Range(0, server.replicas)
                            .Select(i => (Hashcode: MurmurHash2.Hash(server.id, $"{i}"), server)))
                    .OrderBy(x => x.Hashcode).ToArray();
        }

        public double AverageLoad()
        {
            return (InflightRequests * Load) / (double) Servers.Length;
        }

        public Server NextBoundedTryHash(string host, string pathAndQuery)
        {
            var ring = _ring;
            if (ring.Length == 0)
                return null;
            if (ring.Length == 1)
                return ring[0].Server;

            var hash = MurmurHash2.Hash(host, pathAndQuery);
            
            var j = 0;
            var avg = AverageLoad();
            var i = Index(hash);
            while (ring[i].Server.inflightRequest > avg)
            {
                if (j == ring.Length)
                    throw new Exception("No Servers with enough capacity");
                j++;
                hash = MurmurHash2.Hash($"{hash}", $"{j}");
                i = Index(hash);
            }
            
            //ring[i].Server.inflightRequest++;
            //InflightRequests++;
            return ring[i].Server;
        }

        public Server NextBoundedTryNext(string host, string pathAndQuery)
        {
            var ring = _ring;
            if (ring.Length == 0)
                return null;
            if (ring.Length == 1)
                return ring[0].Server;

            var hash = MurmurHash2.Hash(host, pathAndQuery);
            var i = Index(hash);
            var count = 0;
            var avg = AverageLoad();
            while (ring[i].Server.inflightRequest > avg)
            {
                if (count > ring.Length)
                    throw new Exception("No Servers with enough capacity");
                i++;
                if (i == ring.Length)
                    i = 0;
                count++;
            }

            //ring[i].Server.inflightRequest++;
            //InflightRequests++;
            return ring[i].Server;
        }

        public Server Next(string host, string pathAndQuery)
        {
            var ring = _ring;
            if (ring.Length == 0)
                return null;
            if (ring.Length == 1)
                return ring[0].Server;

            var hash = MurmurHash2.Hash(host, pathAndQuery);
            return ring[Index(hash)].Server;
        }

        public int Index(uint hash)
        {
            var ring = _ring;
            var begin = 0;
            var end = ring.Length - 1;

            if (ring[end].Hashcode < hash || ring[0].Hashcode > hash)
                return 0;

            while (end - begin > 1)
            {
                var mid = (end + begin) / 2;
                if (ring[mid].Hashcode >= hash)
                    end = mid;
                else
                    begin = mid;
            }
            Debug.Assert(ring[begin].Hashcode <= hash && ring[end].Hashcode >= hash);

            return end;
        }
    }

    public static class MurmurHash2
    {
        public static uint Hash(string a, string b)
        {
            const uint seed = 0xc58f1a7b;
            const uint m = 0x5bd1e995;
            const int r = 24;
            var data = GetBytes(a, b);
            var length = data.Length;
            if (length == 0)
                return 0;
            var h = seed ^ (uint) length;
            var i = 0;
            var uints = MemoryMarshal.Cast<byte, uint>(data);
            while (length >= 4)
            {
                var k = uints[i++];
                k *= m;
                k ^= k >> r;
                k *= m;
                h *= m;
                h ^= k;
                length -= 4;
            }

            i *= 4;
            switch (length)
            {
                case 3:
                    h ^= (ushort) (data[i++] | data[i++] << 8);
                    h ^= (uint) data[i] << 16;
                    h *= m;
                    break;
                case 2:
                    h ^= (ushort) (data[i++] | data[i] << 8);
                    h *= m;
                    break;
                case 1:
                    h ^= data[i];
                    h *= m;
                    break;
            }

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;
            return h;
        }

        [ThreadStatic] private static byte[] _buffer;

        private static ReadOnlySpan<byte> GetBytes(string a, string b)
        {
            const int max = 16 * 1024;
            var arr = _buffer ??= new byte[max];
            var buffer = arr.AsSpan();
            var size = 0;

            var length = Encoding.UTF8.GetBytes(a, buffer);

            size += length;
            buffer = buffer.Slice(length);

            buffer[0] = (byte) '-'; //"{a}-{b}"
            size++;
            buffer = buffer.Slice(1);

            length = Encoding.UTF8.GetBytes(b, buffer);
            size += length;

            return arr.AsSpan(0, size);
        }
    }
}