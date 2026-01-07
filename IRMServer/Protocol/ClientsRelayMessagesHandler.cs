using IRMShared;

namespace IRMServer.Protocol
{
    public class ClientsRelayMessagesHandler : ClientMessagesHandlerBase
    {
        protected override void HandleOnMessageReceived(ConnectedClientInstance clientFrom, Messages.RawMessage message)
        {
            if (message.Target == EMessageTarget.OTHERS)
            {
                foreach (var client in _server.ConnectedClientsMapMap.Values)
                {
                    if (client.Equals(clientFrom))
                    {
                        continue;
                    }
                    _server.EnqueueMessageToBeSend(client, message);
                }
            }
        }
    }
}