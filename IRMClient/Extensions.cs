using IRMClient.Protocol;

namespace IRMClient
{
    public static class Extensions
    {
        public static ClientInstance SetupDefaultClientHandlers(this ClientInstance clientInstance)
        {
            clientInstance.AddMessageHandler(new UserRegistrationClientHandler(clientInstance));
            clientInstance.AddMessageHandler(new ClientPositionWriteHandler(clientInstance));
            clientInstance.AddMessageHandler(new OthersClientPositionReadHandler(clientInstance));
            return clientInstance;
        }
    }
}