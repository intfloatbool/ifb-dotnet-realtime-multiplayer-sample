using System;
using IRMShared;

namespace IRMServer
{
    public class ServerConfiguration
    {
        public ushort Port;
        public int MaxClients = 10;
        public int ChannelLimit = Enum.GetNames(typeof(EChannel)).Length;
        public int LoopFrequencyDelayMs = 200;
        public int HostServiceTimeoutMs = 200;
        
        /// <summary>
        /// incoming/outgoing (0 = unlimited)
        /// </summary>
        public ValueTuple<uint, uint> Bandwidth;
        public int? BufferSize;
    }
}