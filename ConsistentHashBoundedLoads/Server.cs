using System;

namespace ConsistentHashBoundedLoads
{
    public class Server
    {
        public string Id;
        public int Replicas;
        public AtomicCounter InflightRequests;

        public Server(string id, int replicas)
        {
            Id = id;
            Replicas = replicas;
        }
    }
}