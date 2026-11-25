using Grpc.Net.Client;
using MagicOnion.Client;
using KawaStarter.Contracts.Generated;

using var channel = GrpcChannel.ForAddress(args.FirstOrDefault() ?? "http://localhost:5011");
var client = MagicOnionClient.Create<IChatMagicOnionService>(channel);
Console.WriteLine($"unary: {await client.SendAsync("hello")}");
using (var watch = await client.WatchAsync("hello"))
    while (await watch.ResponseStream.MoveNext(CancellationToken.None))
        Console.WriteLine($"server stream: {watch.ResponseStream.Current}");
using (var upload = await client.UploadAsync())
{
    await upload.RequestStream.WriteAsync("first");
    await upload.RequestStream.WriteAsync("second");
    await upload.RequestStream.CompleteAsync();
    Console.WriteLine($"client stream count: {await upload.ResponseAsync}");
}
using (var exchange = await client.ExchangeAsync())
{
    var producer = Task.Run(async () =>
    {
        await exchange.RequestStream.WriteAsync("duplex hello");
        await exchange.RequestStream.CompleteAsync();
    });
    while (await exchange.ResponseStream.MoveNext(CancellationToken.None))
        Console.WriteLine($"duplex: {exchange.ResponseStream.Current}");
    await producer;
}
var firstReceiver = new Receiver();
var secondReceiver = new Receiver();
await using var first = await StreamingHubClient.ConnectAsync<IChatMagicOnionHub, IChatMagicOnionHubReceiver>(channel, firstReceiver);
await using var second = await StreamingHubClient.ConnectAsync<IChatMagicOnionHub, IChatMagicOnionHubReceiver>(channel, secondReceiver);
await first.HandleAsync("hello lobby");
Console.WriteLine($"hub first: {await firstReceiver.Message.Task.WaitAsync(TimeSpan.FromSeconds(10))}");
Console.WriteLine($"hub second: {await secondReceiver.Message.Task.WaitAsync(TimeSpan.FromSeconds(10))}");

sealed class Receiver : IChatMagicOnionHubReceiver
{
    public TaskCompletionSource<string> Message { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void OnEvent(string serverEvent) => Message.TrySetResult(serverEvent);
}
