using System.Buffers;

namespace MMKiwi.MdbTools.Helpers
{
    internal class MdbPool
    {
        private ArrayPool<byte> Pool { get; }
        public MdbPool(ArrayPool<byte> pool)
        {
            Pool = pool;
        }

        public MdbPoolManager Rent(int length) => new MdbPoolManager(length, Pool);
    }
    internal class MdbPoolManager : IDisposable
    {
#if DEBUG
        static readonly object s_lock = new();
        static int s_numAlloc;
#endif
        public int Length { get; }
        private ArrayPool<byte> Pool { get; }

        internal MdbPoolManager(int length, ArrayPool<byte> pool)
        {
#if DEBUG
            lock (s_lock)
            {
                Console.WriteLine($"Allocating {++s_numAlloc}");
            }
#endif
            Length = length;
            Pool = pool;
            Value = pool.Rent(length);
        }
        public byte[] Value { get; }
        public Span<byte> Trimmed => Value.AsSpan(0, Length);



        public void Dispose()
        {
#if DEBUG
            lock (s_lock)
            {
                Console.WriteLine($"Deallocating {--s_numAlloc}");
            }
#endif
            Pool.Return(Value);
        }
    }
}