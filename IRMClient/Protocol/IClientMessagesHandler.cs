using System;
using IRMShared;
using R3;

namespace IRMClient.Protocol
{
    public interface IClientMessagesHandler : IDisposable
    {
        void HandleSetup(Observable<Messages.RawMessage> messageRecv, Action<Messages.RawMessage> sendMessageAction);
    }
}