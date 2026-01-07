using System;
using IRMShared;
using R3;

namespace IRMClient.Protocol
{
    public abstract class ClientMessageHandlerBase : IClientMessagesHandler
    {
        protected Action<Messages.RawMessage> _sendMessageAction;
        protected CompositeDisposable _compositeDisposable;
        public virtual void HandleSetup(Observable<Messages.RawMessage> messageRecv, Action<Messages.RawMessage> sendMessageAction)
        { 
            _sendMessageAction = sendMessageAction; 
            _compositeDisposable?.Clear();
            _compositeDisposable = new CompositeDisposable(
                messageRecv.Subscribe(HandleOnMessageReceived));
        }

        protected abstract void HandleOnMessageReceived(Messages.RawMessage message);
        
        public virtual void Dispose()
        {
            _compositeDisposable?.Clear();
        }
    }
}