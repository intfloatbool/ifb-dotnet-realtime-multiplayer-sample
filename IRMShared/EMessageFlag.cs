namespace IRMShared
{
    public enum EMessageFlag : byte
    {
        /// <summary>
        /// unreliable sequenced, delivery of packet is not guaranteed.
        /// </summary>
        NONE, 
        
        /// <summary>
        /// reliable sequenced, a packet must be received by the target peer and resend attempts should be made until the packet is delivered.
        /// </summary>
        RELIABLE,  
        
        /// <summary>
        /// a packet will be unreliably fragmented if it exceeds the MTU. By default, unreliable packets that exceed the MTU are fragmented and transmitted reliably. This flag should be used to explicitly indicate packets that should remain unreliable.
        /// </summary>
        UNRELIABLE,
        
        /// <summary>
        ///  a packet will not be sequenced with other packets and may be delivered out of order. This flag makes delivery unreliable.
        /// </summary>
        UNSEQUENCED,
        
        /// <summary>
        /// a packet will not be bundled with other packets at a next service iteration and sent instantly instead. This delivery type trades multiplexing efficiency in favor of latency. The same packet can't be used for multiple Peer.Send() calls.
        /// </summary>
        INSTANT
    }
}