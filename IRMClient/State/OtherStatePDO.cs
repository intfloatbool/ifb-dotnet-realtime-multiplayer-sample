using System;

namespace IRMClient.State
{
    public struct OtherStatePDO
    {
        public readonly int UserId;
        public readonly ulong Tick;
        public readonly IRMVec3 Pos;
        public readonly DateTime SentTimestamp;

        public OtherStatePDO(int userId, ulong tick, IRMVec3 pos, DateTime sentTime)
        {
            UserId = userId;
            Tick = tick;
            Pos = pos;
            SentTimestamp = sentTime;
        }
    }
}