using Grpc.Net.Client;
using Kawa.Sample.Grpc.Contracts.Proto;

using var channel = GrpcChannel.ForAddress(args.FirstOrDefault() ?? "http://localhost:5021");
var client = new Chat.ChatClient(channel);
Console.WriteLine($"unary: {(await client.SendAsync(new SendRequest { Message = "hello" })).Message}");
using (var watch = client.Watch(new WatchRequest { Message = "hello" }))
    while (await watch.ResponseStream.MoveNext(CancellationToken.None))
        Console.WriteLine($"server stream: {watch.ResponseStream.Current.Message}");
using (var upload = client.Sum())
{
    await upload.RequestStream.WriteAsync(new SumRequest { Value = 1 });
    await upload.RequestStream.WriteAsync(new SumRequest { Value = 2 });
    await upload.RequestStream.CompleteAsync();
    Console.WriteLine($"client stream sum: {(await upload.ResponseAsync).Total}");
}
using (var exchange = client.EchoStream())
{
    var producer = Task.Run(async () =>
    {
        await exchange.RequestStream.WriteAsync(new EchoRequest { Message = "duplex hello" });
        await exchange.RequestStream.CompleteAsync();
    });
    while (await exchange.ResponseStream.MoveNext(CancellationToken.None))
        Console.WriteLine($"duplex: {exchange.ResponseStream.Current.Message}");
    await producer;
}
