using IRMShared;
using R3;

namespace IRMServer.Protocol
{
    public abstract class ClientMessagesHandlerBase : IHandler
    {
        protected IServer _server;
        protected CompositeDisposable _compositeDisposable;

        public void Handle(IServer server)
        {
            _server = server;
            _compositeDisposable?.Clear();
            _compositeDisposable = new CompositeDisposable(
                _server.MessageReceivedFrom.Subscribe(HandleOnMessageReceivedFrom)
            );
        }
        
        public void Dispose()
        {
           _compositeDisposable?.Clear();
        }

        private void HandleOnMessageReceivedFrom((ConnectedClientInstance, Messages.RawMessage) md)
        {
            HandleOnMessageReceived(md.Item1, md.Item2);
        }

        protected abstract void HandleOnMessageReceived(ConnectedClientInstance clientFrom, Messages.RawMessage message);
    }
}