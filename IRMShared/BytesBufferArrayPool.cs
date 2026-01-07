using System;
using System.Collections.Concurrent;

namespace IRMShared
{
    public class BytesBufferArrayPool
    {
        private readonly int _bufferSize;
        private readonly ConcurrentQueue<byte[]> _pool;

        public BytesBufferArrayPool(int bufferSize, int prewarmCount = 0)
        {
            _bufferSize = bufferSize;
            _pool = new ConcurrentQueue<byte[]>();
            if (prewarmCount > 0)
            {
                Prewarm(prewarmCount);
            }
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var buff = new byte[_bufferSize];
                _pool.Enqueue(buff);
            }
        }

        public void Return(byte[] buffer)
        {
            Array.Clear(buffer, 0, _bufferSize);
            _pool.Enqueue(buffer);
        }

        public byte[] Rent()
        {
            if (_pool.TryDequeue(out var res))
            {
                return res;
            }
            
            var buff = new byte[_bufferSize];

            return buff;
        }
    }
}