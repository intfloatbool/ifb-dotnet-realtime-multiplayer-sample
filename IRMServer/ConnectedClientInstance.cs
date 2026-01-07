using ENet;

namespace IRMServer
{
    public class ConnectedClientInstance
    {
        public readonly Peer Peer;
        public readonly uint ID;
        public ConnectedClientInstance(ref Peer peer)
        {
            ID = peer.ID;
            Peer = peer;
        }
    }
}