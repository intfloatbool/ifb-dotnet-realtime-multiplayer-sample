using System;

namespace IRMServer.Protocol
{
    public interface IHandler : IDisposable
    {
        void Handle(IServer server);
    }
}