using System.Threading;

namespace ConsistentHashBoundedLoads
{
    public sealed class AtomicCounter
    {
        private int _value;

        public int Value
        {
            get => Volatile.Read(ref _value);
            set => Volatile.Write(ref _value, value);
        }

        public int Increment() => Interlocked.Increment(ref _value);

        public int Decrement() => Interlocked.Decrement(ref _value);
    }
}