using System.Collections.Generic;
using IRMShared;
using R3;

namespace IRMServer
{
    public interface IServer
    {
        Observable<(ConnectedClientInstance, Messages.RawMessage)> MessageReceivedFrom { get; }
        IReadOnlyDictionary<uint, ConnectedClientInstance> ConnectedClientsMapMap { get; }
        Observable<ConnectedClientInstance> ClientConnected { get; }
        Observable<ConnectedClientInstance> ClientDisconnected { get; }
        void EnqueueMessageToBeSend(ConnectedClientInstance target, Messages.RawMessage message);
    }
}