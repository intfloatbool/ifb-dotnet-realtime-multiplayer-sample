using IRMClient;
using IRMClient.State;
using IRMServer;
using IRMServer.Protocol;
using IRMShared;
using MessagePack;
using R3;

namespace IRMTests;

public class IntegrationalTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test, Order(1)]
    public async Task ServerInstanceStartingWithoutExceptions()
    {
        var serverInstance = new ServerInstance();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await serverInstance.StartAsync(new ServerConfiguration
            {
                Port = 7777,
                MaxClients = 10,
                ChannelLimit = 3
            }, cts.Token);
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                Assert.Fail($"Exception occured: ${ex}");
            }
        }
        finally
        {
            cts.Dispose();
            serverInstance.Dispose();
        }
        
        Assert.Pass();
    }

    [Test, Order(2)]
    public async Task ClientInitialConnectToServer()
    {
        var serverInstance = new ServerInstance();
        var clientInstance = new ClientInstance();
        
        var testCts = new CancellationTokenSource();
        var serverCts = new CancellationTokenSource();
        var clientCts = new CancellationTokenSource();
        List<Exception> serverExceptions = new List<Exception>();
        List<Exception> clientExceptions = new List<Exception>();

        bool isClientConnectedSuccessfully = false;
        bool isClientDisconnectedSuccessfully = false;
        
        try
        {
            _ = serverInstance.StartAsync(new ServerConfiguration
            {
                Port = 7777,
                MaxClients = 10,
                ChannelLimit = 3
            }, serverCts.Token, (ex) =>
            {
                if (ex is not OperationCanceledException)
                {
                    serverExceptions.Add(ex);
                }
            });

            while (!serverInstance.IsReady.CurrentValue)
            {
                await Task.Delay(100, testCts.Token);
            }

            _ = clientInstance.StartAsync(new ClientConfiguration(), "127.0.0.1", 7777, clientCts.Token, (ex) =>
            {
                if (ex is not OperationCanceledException)
                {
                    clientExceptions.Add(ex);
                }
            });

            while (!clientInstance.IsReady.CurrentValue)
            {
                await Task.Delay(100, testCts.Token);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), testCts.Token);

            isClientConnectedSuccessfully = serverInstance.ConnectedClientsMapMap.Count > 0;
            
            await clientCts.CancelAsync();
            
            await Task.Delay(TimeSpan.FromSeconds(1), testCts.Token);

            isClientDisconnectedSuccessfully = serverInstance.ConnectedClientsMapMap.Count == 0;
            
            await serverCts.CancelAsync();
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                Assert.Fail($"Exception occured: ${ex}");
            }
        }
        finally
        {
            testCts.Cancel();
            testCts.Dispose();
            serverCts.Dispose();
            clientCts.Dispose();
            serverInstance.Dispose();
            clientInstance.Dispose();
        }
        Assert.That(serverExceptions, Is.Empty);
        Assert.That(clientExceptions, Is.Empty);
        Assert.That(isClientConnectedSuccessfully, Is.True);
        Assert.That(isClientDisconnectedSuccessfully, Is.True);
    }
    
    [Test, Order(5)]
    public async Task ClientRegistrationSuccess()
    {
        ushort port = 7780;
        var cts = new CancellationTokenSource();
        var (server, allClients) = await CreateServerAndThreeClientsAsync(cts.Token, port);

        await WaitRegistrationForClientsAsync(allClients);
        
        Assert.That(allClients.Count, Is.EqualTo(3));
        
        Assert.That(allClients.Count((c) => c.ClientState.UserInfo.IsMaster), Is.EqualTo(1));
        
        Assert.That(allClients.Count((c) => c.ClientState.UserInfo.Id == 0), Is.EqualTo(1));
        
        Assert.That(allClients.Count((c) => c.ClientState.UserInfo.Id == 1), Is.EqualTo(1));
        
        Assert.That(allClients.Count((c) => c.ClientState.UserInfo.Id == 2), Is.EqualTo(1));
        

        allClients.ForEach(c => c.Dispose());

        server.Dispose();
    }

    [Test]
    public async Task ClientsStateSync()
    {
        ushort port = 7781;
        
        var cts = new CancellationTokenSource();
        var (server, allClients) = await CreateServerAndThreeClientsAsync(cts.Token, port);
        
        await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

        var (clientA, clientB, clientC) = (allClients[0], allClients[1], allClients[2]);

        await WaitRegistrationForClientsAsync(allClients);
        
        var posClientA = new IRMVec3(10, 0, 0);
        var posClientB = new IRMVec3(0, 10, 0);
        var posClientC = new IRMVec3(0, 0, -10);
        
        for (int i = 0; i < 5; i++)
        {
            clientA.ClientState.Position.Value = posClientA;
            posClientA.X++;
            await Task.Delay(50, cts.Token);
            
            clientB.ClientState.Position.Value = posClientB;
            posClientB.Y++;
            await Task.Delay(50, cts.Token);
            
            clientC.ClientState.Position.Value = posClientC;
            posClientC.Z++;
            await Task.Delay(50, cts.Token);
        }
        
        
        await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
        
        Assert.That(clientA.ClientState.OthersStatesCollection.Count, Is.EqualTo(2));
        Assert.That(clientB.ClientState.OthersStatesCollection.Count, Is.EqualTo(2));
        Assert.That(clientC.ClientState.OthersStatesCollection.Count, Is.EqualTo(2));
        
        Assert.That(clientA.ClientState.OthersStatesCollection.FirstOrDefault(s => s.UserId.Value == clientB.ClientState.UserInfo.Id), Is.Not.Null);
        Assert.That(clientA.ClientState.OthersStatesCollection.FirstOrDefault(s => s.UserId.Value == clientC.ClientState.UserInfo.Id), Is.Not.Null);
        
        Assert.That(clientB.ClientState.OthersStatesCollection.FirstOrDefault(s => s.UserId.Value == clientA.ClientState.UserInfo.Id), Is.Not.Null);
        Assert.That(clientB.ClientState.OthersStatesCollection.FirstOrDefault(s => s.UserId.Value == clientC.ClientState.UserInfo.Id), Is.Not.Null);
        
        Assert.That(clientC.ClientState.OthersStatesCollection.FirstOrDefault(s => s.UserId.Value == clientA.ClientState.UserInfo.Id), Is.Not.Null);
        Assert.That(clientC.ClientState.OthersStatesCollection.FirstOrDefault(s => s.UserId.Value == clientB.ClientState.UserInfo.Id), Is.Not.Null);
        
        allClients.ForEach(c => c.Dispose());
        server.Dispose();
        cts.Cancel();
        cts.Dispose();
    } 
    
    [Test]
    public async Task ClientOthersMessagesQueue()
    {
        ushort port = 7781;
        
        var cts = new CancellationTokenSource();
        var (server, allClients) = await CreateServerAndThreeClientsAsync(cts.Token, port);
        
        await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

        var (clientA, clientB, clientC) = (allClients[0], allClients[1], allClients[2]);

        await WaitRegistrationForClientsAsync(allClients);
        
        var posClientA = new IRMVec3(10, 0, 0);
        var posClientB = new IRMVec3(0, 10, 0);
        var posClientC = new IRMVec3(0, 0, -10);
        
        for (int i = 0; i < 5; i++)
        {
            clientA.ClientState.Position.Value = posClientA;
            posClientA.X++;
            await Task.Delay(50, cts.Token);
            
            clientB.ClientState.Position.Value = posClientB;
            posClientB.Y++;
            await Task.Delay(50, cts.Token);
            
            clientC.ClientState.Position.Value = posClientC;
            posClientC.Z++;
            await Task.Delay(50, cts.Token);
        }
        
        
        await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
        
        Assert.That(clientA.ClientState.OthersStateMessagesQueue.Count(), Is.GreaterThan(0));
        Assert.That(clientB.ClientState.OthersStateMessagesQueue.Count(), Is.GreaterThan(0));
        Assert.That(clientC.ClientState.OthersStateMessagesQueue.Count(), Is.GreaterThan(0));
        
        allClients.ForEach(c => c.Dispose());
        server.Dispose();
        cts.Cancel();
        cts.Dispose();
    } 

    private async Task<(ServerInstance, List<ClientInstance>)> CreateServerAndThreeClientsAsync(CancellationToken token, ushort port)
    {
        var (clientA, server) = await CreateServerAndClientPairAsync(token, port);
        var clientB = await CreateConnectedClient(server, port, token);
        var clientC = await CreateConnectedClient(server, port, token);

        clientA.SetupDefaultClientHandlers();
        clientB.SetupDefaultClientHandlers();
        clientC.SetupDefaultClientHandlers();

        return (server, new List<ClientInstance>
        {
            clientA, clientB, clientC
        });
    }

    private async Task<(ClientInstance, ServerInstance)> CreateServerAndClientPairAsync(CancellationToken token, ushort port)
    {
        var clientInstance = new ClientInstance();
        var serverInstance = new ServerInstance();
        serverInstance.SetupDefaultBeforeStartServerHandlers();
        _ = serverInstance.StartAsync(new ServerConfiguration
        {
            Port = port,
            MaxClients = 10,
            ChannelLimit = 3
        }, token, (ex) =>
        {
            if (ex is not OperationCanceledException)
            {
                throw ex;
            }
        });

        using var serverReadyCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); 
        
        while (!serverInstance.IsReady.CurrentValue)
        {
            try
            {
                await Task.Delay(100, serverReadyCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new Exception("server isReady timeout");
            }
        }
        using var clientReadyCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); 
        _ = clientInstance.StartAsync(new ClientConfiguration(), "127.0.0.1", port, token, (ex) =>
        {
            if (ex is not OperationCanceledException)
            {
                throw ex;
            }
        });

        while (!clientInstance.IsReady.CurrentValue)
        {
            try
            {
                await Task.Delay(100, clientReadyCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new Exception("client isReady timeout");
            }
        }

        await Task.Delay(TimeSpan.FromSeconds(1), token);

        return (clientInstance, serverInstance);

    }

    private async Task<ClientInstance> CreateConnectedClient(ServerInstance server, ushort port, CancellationToken token)
    {
        var clientInstance = new ClientInstance();
        using var clientReadyCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); 
        _ = clientInstance.StartAsync(new ClientConfiguration(), "127.0.0.1", port, token, (ex) =>
        {
            if (ex is not OperationCanceledException)
            {
                throw ex;
            }
        });

        while (!clientInstance.IsReady.CurrentValue)
        {
            try
            {
                await Task.Delay(100, clientReadyCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new Exception("client isReady timeout");
            }
        }

        await Task.Delay(TimeSpan.FromSeconds(1), token);

        return clientInstance;
    }

    private async Task WaitRegistrationForClientsAsync(IEnumerable<ClientInstance> allClients, int seconds = 5)
    {
        var registrationTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        try
        {
            
            var registrationTasks = allClients
                .Select(c => c.WaitForRegistrationSuccessAsync(registrationTimeoutCts.Token))
                .ToArray();

            await Task.WhenAll(registrationTasks);
        }
        finally
        {
            registrationTimeoutCts.Cancel();
            registrationTimeoutCts.Dispose();
        }
    }
}