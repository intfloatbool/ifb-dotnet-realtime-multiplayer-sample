using System.Linq;
using IRMClient.State;
using IRMShared;
using MessagePack;
using ObservableCollections;
using R3;

namespace IRMClient.Protocol
{
    public class OthersClientPositionReadHandler : ClientMessageHandlerBase
    {
        private readonly IClientStateHolder _clientStateHolder;
        private readonly ObservableList<OthersState> _othersCollection;

        public OthersClientPositionReadHandler(IClientStateHolder clientStateHolder)
        {
            _clientStateHolder = clientStateHolder;
            _othersCollection = _clientStateHolder.ClientState.OthersStatesCollection;
        }

        protected override void HandleOnMessageReceived(Messages.RawMessage message)
        {
            if (message.MessageType != EMessageType.CLIENT_STATE_UPDATED)
            {
                return;
            }
            
            if (!_clientStateHolder.ClientState.UserInfo.IsReady)
            {
                //Console.WriteLine("XXX OthersClientPositionReadHandler() userInfo is not READY");
                return;
            }
            
            var clientMessage = MessagePackSerializer.Deserialize<Messages.ClientStateMessage>(message.BodyData);

            var existedState = _othersCollection.FirstOrDefault(s => s.UserId.Value == clientMessage.ClientId);

            var pos = new IRMVec3(clientMessage.PosX, clientMessage.PosY, clientMessage.PosZ);
            //Console.WriteLine($"CLIENT_XXX Update other position, my id: {_clientStateHolder.ClientState.UserInfo.Id} , otherId: {clientMessage.ClientId} , pos: {pos}");
            //Console.WriteLine($"XXX OthersClientPositionReadHandler() myID: {_clientStateHolder.ClientState.UserInfo.Id} existedState: {existedState?.UserId?.Value.ToString() ?? "none"}");
            
            if (existedState != null)
            {
                existedState.Tick.Value = clientMessage.Tick;
                existedState.Pos.Value = pos;
                existedState.Updated.OnNext(Unit.Default);
            }
            else
            {
                var newClientState = new OthersState();
                newClientState.UserId.Value = clientMessage.ClientId;
                newClientState.Tick.Value = clientMessage.Tick;
                newClientState.Pos.Value = new IRMVec3(clientMessage.PosX, clientMessage.PosY, clientMessage.PosZ);
                newClientState.Updated.OnNext(Unit.Default);
                _othersCollection.Add(newClientState);
            }
            
            _clientStateHolder.ClientState.OthersStateMessagesQueue.Enqueue(new OtherStatePDO(clientMessage.ClientId, clientMessage.Tick, pos, clientMessage.SentTimestamp));
        }
    }
}