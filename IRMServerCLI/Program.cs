// See https://aka.ms/new-console-template for more information

using IRMServer;

var server = new ServerInstance();
server.SetupDefaultBeforeStartServerHandlers();
var cts = new CancellationTokenSource();
_ = server.StartAsync(new ServerConfiguration
{
    Port = 7777,
    MaxClients = 10,
    ChannelLimit = 3
}, CancellationToken.None, (ex) =>
{
    Console.Error.Write($"server err! {ex}");
});

Console.WriteLine("Server started. Press Enter to kill.");

Console.ReadKey();

Console.WriteLine("Done");

cts.Cancel();
cts.Dispose();
server.Dispose();

