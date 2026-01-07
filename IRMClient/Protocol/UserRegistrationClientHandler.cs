using System;
using System.Linq;
using IRMClient.State;
using IRMShared;
using MessagePack;
using R3;

namespace IRMClient.Protocol
{
    public class UserRegistrationClientHandler : ClientMessageHandlerBase
    {
        private readonly IClientStateHolder _clientStateHolder;

        public UserRegistrationClientHandler(IClientStateHolder clientStateHolder)
        {
            _clientStateHolder = clientStateHolder;
        }

        public override void HandleSetup(Observable<Messages.RawMessage> messageRecv, Action<Messages.RawMessage> sendMessageAction)
        {
            base.HandleSetup(messageRecv, sendMessageAction);
            
            //Console.WriteLine($"\tCLIENT_XXX HandleSetup() REGISTER REQ client: {GetHashCode()}");
            _sendMessageAction(new Messages.RawMessage
            {
                Target = EMessageTarget.SERVER,
                Channel = EChannel.CRITICAL,
                MessageType = EMessageType.REQ_USER_INFO,
                MessageFlag = EMessageFlag.RELIABLE,
            });
        }

        protected override void HandleOnMessageReceived(Messages.RawMessage message)
        {
            if (message.MessageType == EMessageType.RES_USER_INFO)
            {
                OnRegistrationSuccess(message);
                return;
            }

            if (message.MessageType == EMessageType.ALL_CLIENTS_INFO)
            {
                OnAllClientsInfoUpdated(message);
                return;
            }
            
        }

        private void OnRegistrationSuccess(Messages.RawMessage message)
        {
            var userInfoDeserialized = MessagePackSerializer.Deserialize<Messages.ResponseUserInfoBody>(message.BodyData!);

            _clientStateHolder.ClientState.UserInfo.Id = userInfoDeserialized.Id;
            _clientStateHolder.ClientState.UserInfo.IsMaster = userInfoDeserialized.IsMaster;
            _clientStateHolder.ClientState.UserInfo.IsReady = true;
            
            IRMLogger.LogMsg($"CLIENT_XXX OnRegistrationSuccess()! My id: {_clientStateHolder.ClientState.UserInfo.Id}");
            
        }

        private void OnAllClientsInfoUpdated(Messages.RawMessage message)
        {
            if (!_clientStateHolder.ClientState.UserInfo.IsReady)
            {
                return;
            }
            var allClientsInfo = MessagePackSerializer.Deserialize<Messages.AllClientsInfo>(message.BodyData);
            if (allClientsInfo.UserInfoBodyCollection.Length == 0)
            {
                IRMLogger.LogErr($"Client: [{GetType().Name}] OnAllClientsInfoUpdated UserInfoBodyCollection is empty!");
                return;
            }

            _clientStateHolder.ClientState.OthersStatesCollection.Clear();
            foreach (var clientInfo in allClientsInfo.UserInfoBodyCollection)
            {
                if (clientInfo.Id == _clientStateHolder.ClientState.UserInfo.Id)
                {
                    continue;
                }

                var state = new OthersState();
                state.UserId.Value = clientInfo.Id;
                _clientStateHolder.ClientState.OthersStatesCollection.Add(state);
            }
        }
    }
}