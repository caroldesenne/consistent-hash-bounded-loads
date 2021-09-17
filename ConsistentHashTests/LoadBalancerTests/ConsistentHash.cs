using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LoadBalancerTests
{

    public class Server
    {
        public string id { get; }
        public int inflightRequest { get; }
        public int replicas { get; }
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
        public static Server Next(Server[] servers, double load, string host, string pathAndQuery, string mode)
        {
            var ring =
                servers
                    .OrderBy(x => x.id)
                    .SelectMany(server =>
                        Enumerable.Range(0, server.replicas)
                            .Select(i => (Hashcode: MurmurHash2.Hash(server.id, $"{i}"), server)))
                    .OrderBy(x => x.Hashcode).ToArray();
            var inflight = servers.Select(x => x.inflightRequest).Sum();
            var avg = (inflight * load) / (double) servers.Length;
            
            if (ring.Length == 0)
                return null;
            if (ring.Length == 1)
                return ring[0].server;
            
            var hash = MurmurHash2.Hash(host, pathAndQuery);

            if (mode == "vanilla")
            {
                var i = Index(ring, hash);
                return ring[i].server;
            }
            if (mode == "google")
            {
                var i = Index(ring, hash);
                var count = 0;
                while (ring[i].server.inflightRequest > avg)
                {
                    if (count > ring.Length)
                        throw new Exception("No Servers with enough capacity");
                    i++;
                    if (i == ring.Length)
                        i = 0;
                    count++;
                }
                return ring[i].server;
            }
            if (mode == "jumps")
            {
                var j = 0;
                var i = Index(ring, hash);
                while (ring[i].server.inflightRequest > avg)
                {
                    if (j == ring.Length)
                        throw new Exception("No Servers with enough capacity");
                    j++;
                    hash = MurmurHash2.Hash($"{hash}", $"{j}");
                    i = Index(ring, hash);
                }
                return ring[i].server;
            }
            return null;
        }
        
        public static int Index((uint Hashcode, Server Server)[] ring, uint hash)
        {
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