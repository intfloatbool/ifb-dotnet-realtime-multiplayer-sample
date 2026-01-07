using System;
using IRMShared;

namespace IRMClient
{
    public class ClientConfiguration
    {
        public int ChannelLimit = Enum.GetNames(typeof(EChannel)).Length;
        public int LoopFrequencyDelayMs = 200;
        public int HostServiceTimeoutMs = 200;
    }
}