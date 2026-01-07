using System;
using ENet;
using IRMServer.Protocol;

namespace IRMServer
{
    public static class Extensions
    {
        public static ServerInstance SetupDefaultBeforeStartServerHandlers(this ServerInstance serverInstance)
        {
            return serverInstance
                .AddHandlerBeforeStart(new UserRegistrationHandler())
                .AddHandlerBeforeStart(new ClientsRelayMessagesHandler());
        }

        public static IntPtr GetNativeDataFromPeer(ref Peer peer)
        {
            
            return IntPtr.Zero;
        }
    }
}