// See https://aka.ms/new-console-template for more information


using IRMClient;
using IRMClient.State;
using IRMShared;

if (args.Length < 3)
{
    throw new ArgumentException($"Arguments invalid, expected: [ 'host' , 'port' , 'client count' ], but got: {string.Join(',', args)}");
}

var host = args[0];
var port = int.Parse(args[1]);
var clientCount = int.Parse(args[2]);

IRMLogger.Setup(Console.WriteLine, Console.Error.WriteLine);
IRMLogger.IsLoggingEnabled = true;
var cts = new CancellationTokenSource();

async Task StartClientAsync(CancellationToken token)
{
    var clientInstance = new ClientInstance();
    
    clientInstance.SetupDefaultClientHandlers();
    using var clientReadyCts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); 
    _ = clientInstance.StartAsync(new ClientConfiguration(), host, (ushort) port, token, (ex) =>
    {
        if (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"ClientInstance.StartAsync() ERR! {ex}");
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
    
    var registerTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    try
    {
        await clientInstance.WaitForRegistrationSuccessAsync(registerTimeoutCts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("WaitForRegistrationSuccessAsync timeout!");
    }
    finally
    {
        registerTimeoutCts.Dispose();
    }

    var random = new Random();
    random.NextSingle();
    ulong tick = 0;
    while (!token.IsCancellationRequested)
    {
        bool sign = tick % 2 == 0;
        IRMVec3 pos = default;
        if (sign)
        {
            pos = new IRMVec3(
                random.NextSingle() * -10,
                0,
                random.NextSingle() * 10
            );
            
        }
        else
        {
            pos = new IRMVec3(
                random.NextSingle() * 10,
                0,
                random.NextSingle() * -10
            );
        }

        clientInstance.ClientState.Position.Value = pos;
        tick++;
        await Task.Delay(500, token);
    }
}

Task[] clientTasks = new Task[clientCount];
for (int i = 0; i < clientCount; i++)
{
    await Task.Delay(TimeSpan.FromSeconds(2));
    clientTasks[i] = StartClientAsync(cts.Token);
}

Console.WriteLine("...press any key to cancel.");

Console.ReadKey();

cts.Cancel();
cts.Dispose();

Console.WriteLine("Finish.");