using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IRMClient.State;

namespace IRMClient
{
    public class OthersStateMessagesQueue
    {
        private readonly ConcurrentQueue<OtherStatePDO> _queue;
        private readonly int _maxCount;

        public OthersStateMessagesQueue(int maxCount)
        {
            _maxCount = maxCount;
            _queue = new ConcurrentQueue<OtherStatePDO>();
        }

        public int Count()
        {
            return _queue.Count;
        }

        public void Enqueue(OtherStatePDO otherState)
        {
            if (_queue.Count >= _maxCount)
            {
                _queue.Clear();
            }
            _queue.Enqueue(otherState);
        }

        public bool TryDequeue(out OtherStatePDO otherState)
        {
            if (_queue.TryDequeue(out otherState))
            {
                return true;
            }

            return false;
        }
        
        public IEnumerable<OtherStatePDO> DequeueAll() 
        {
            while (!_queue.IsEmpty)
            {
                if (_queue.TryDequeue(out var otherState))
                {
                    yield return otherState;
                }
            }
        }
    }
}