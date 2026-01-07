using System;
using System.Collections.Generic;
using System.Linq;
using IRMShared;
using MessagePack;
using R3;

namespace IRMServer.Protocol
{
    public sealed class UserRegistrationHandler : IHandler
    {
        private IServer _server;
        private CompositeDisposable _compositeDisposable;
        private Dictionary<ConnectedClientInstance, ValueTuple<int, bool>> _registeredPlayers = new Dictionary<ConnectedClientInstance, ValueTuple<int, bool>>();

        public void Handle(IServer server)
        {
            _server = server;
            _compositeDisposable?.Clear();
            _compositeDisposable = new CompositeDisposable(
                _server.MessageReceivedFrom.Subscribe(HandleOnMessageReceivedFrom),
                _server.ClientDisconnected.Subscribe(HandleOnClientDisconnected)
            );
        }

        private void HandleOnClientDisconnected(ConnectedClientInstance clientInstance)
        {
            _registeredPlayers.Remove(clientInstance);

            int i = 0;
            foreach (var k in _registeredPlayers.Keys)
            {
                bool isMaster = i == 0;
                var newData = (i, isMaster);
                _registeredPlayers[k] = newData;
                
                var userInfo = new Messages.ResponseUserInfoBody
                {
                    Id = i,
                    IsMaster = isMaster
                };
            
                var response = new Messages.RawMessage
                {
                    MessageType = EMessageType.RES_USER_INFO,
                    Channel = EChannel.CRITICAL,
                    MessageFlag = EMessageFlag.RELIABLE,
                    BodyData = MessagePack.MessagePackSerializer.Serialize(userInfo)
                };
                
                _server.EnqueueMessageToBeSend(k, response);
                
                i++;
            }
            
            OnClientsInfoUpdated();
        }

        private void HandleOnMessageReceivedFrom((ConnectedClientInstance, Messages.RawMessage) md)
        {
            var (client, msg) = md;

            if (msg.MessageType != EMessageType.REQ_USER_INFO)
            {
                return;
            }
            
            //Console.WriteLine($"SERV_XXX Request User Info from client: {client.ID}");
            
            var clientId = _registeredPlayers.Count;
            bool isMaster = clientId == 0;
            var userInfo = new Messages.ResponseUserInfoBody
            {
                Id = clientId,
                IsMaster = isMaster
            };
            
            var response = new Messages.RawMessage
            {
                MessageType = EMessageType.RES_USER_INFO,
                Channel = EChannel.CRITICAL,
                BodyData = MessagePack.MessagePackSerializer.Serialize(userInfo),
                MessageFlag = EMessageFlag.INSTANT
            };

            _registeredPlayers[client] = (clientId, isMaster);
            _server.EnqueueMessageToBeSend(client, response);

            OnClientsInfoUpdated();
        }

        private void OnClientsInfoUpdated()
        {
            var allClientsInfoMessage = new Messages.AllClientsInfo
            {
                UserInfoBodyCollection = _registeredPlayers.Values.Select((t) =>
                {
                    return new Messages.ResponseUserInfoBody
                    {
                        Id = t.Item1,
                        IsMaster = t.Item2
                    };
                }).ToArray()
            };
            var emitMessage = new Messages.RawMessage
            {
                MessageType = EMessageType.ALL_CLIENTS_INFO,
                Target = EMessageTarget.ALL,
                MessageFlag = EMessageFlag.RELIABLE,
                BodyData = MessagePackSerializer.Serialize(allClientsInfoMessage)
            };
            foreach (var c in _server.ConnectedClientsMapMap.Values)
            {
                _server.EnqueueMessageToBeSend(c, emitMessage);
            }
        }
        
        public void Dispose()
        {
            _compositeDisposable.Clear();
        }
    }
}