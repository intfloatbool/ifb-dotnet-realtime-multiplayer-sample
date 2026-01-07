using System;
using IRMClient.State;
using IRMShared;
using MessagePack;
using R3;

namespace IRMClient.Protocol
{
    public class ClientPositionWriteHandler : ClientMessageHandlerBase
    {
        private readonly IClientStateHolder _clientStateHolder;
        private ulong _tick;

        public ClientPositionWriteHandler(IClientStateHolder clientStateHolder)
        {
            _clientStateHolder = clientStateHolder;
        }
        
        public override void HandleSetup(Observable<Messages.RawMessage> messageRecv, Action<Messages.RawMessage> sendMessageAction)
        {
            base.HandleSetup(messageRecv, sendMessageAction);
            _compositeDisposable.Add(_clientStateHolder.ClientState.Position.Subscribe(HandleOnPositionUpdated));
            
        }

        private void HandleOnPositionUpdated(IRMVec3 pos)
        {
            if (!_clientStateHolder.ClientState.UserInfo.IsReady)
            {
                return;
            }
            
            var userId = _clientStateHolder.ClientState.UserInfo.Id;
            var clientStateMessage = new Messages.ClientStateMessage
            {
                ClientId = userId,
                Tick = _tick,
                PosX = pos.X,
                PosY = pos.Y,
                PosZ = pos.Z,
                SentTimestamp = DateTime.UtcNow
            };
            
            _sendMessageAction?.Invoke(new Messages.RawMessage
            {
                MessageType = EMessageType.CLIENT_STATE_UPDATED,
                Target = EMessageTarget.OTHERS,
                Channel = EChannel.STREAM,
                MessageFlag = EMessageFlag.UNRELIABLE,
                BodyData = MessagePackSerializer.Serialize(clientStateMessage),
                //DebugFrom = $"[ClientPositionWriteHandler] id: {userId}"
            });

            //IRMLogger.LogMsg($"CLIENT_XXX update my  position! My id: {_clientStateHolder.ClientState.UserInfo.Id} , pos: {pos}");
            _tick++;
        }
        
        protected override void HandleOnMessageReceived(Messages.RawMessage message)
        {
            
        }
    }
}