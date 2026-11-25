using System.CommandLine;
using Grpc.Net.Client;
using Kawa.Abstractions;
using Kawa.Cli;
using Kawa.Sample.Aspire.ServiceDefaults;
using Kawa.Sample.MagicOnion.Contracts.Generated;
using MagicOnion.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proto = Kawa.Sample.Grpc.Contracts.Proto;

var builder = Host.CreateApplicationBuilder();
builder.Logging.ClearProviders(); // Keep diagnostic logs out of JSON stdout.
builder.AddSampleDefaults();
builder.Services.AddScoped<IUseCase<SendRequest, SendReply>, SendRemoteMessage>();
using var host = builder.Build();
var message = new Option<string>("--message") { DefaultValueFactory = _ => "hello" };
var transport = new Option<string>("--transport") { DefaultValueFactory = _ => "magic" };
var command = new RootCommand("Send through a discovered MagicOnion or protobuf gRPC service.");
command.Options.Add(message);
command.Options.Add(transport);
command.SetKawaAction<SendRequest, SendReply>(host.Services,
    parse => new SendRequest(parse.GetValue(message)!, parse.GetValue(transport)!));
await host.StartAsync();
try
{
    return await command.InvokeKawaAsync(args);
}
finally
{
    await host.StopAsync();
}

public sealed record SendRequest(string Message, string Transport);
public sealed record SendReply(string Message);

public sealed class SendRemoteMessage(IHttpClientFactory clients) : IUseCase<SendRequest, SendReply>
{
    public async Task<KawaResult<SendReply>> ExecuteAsync(SendRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Transport is not ("magic" or "grpc"))
            return KawaResult<SendReply>.Failure(new KawaError(KawaErrorKind.Validation, "Transport must be magic or grpc."));
        using var http = clients.CreateClient("chat");
        using var channel = GrpcChannel.ForAddress(
            request.Transport == "magic" ? "http://_grpc.chat" : "http://_grpc.proto",
            new GrpcChannelOptions { HttpClient = http });
        var response = request.Transport == "magic"
            ? await MagicOnionClient.Create<IChatMagicOnionService>(channel).WithCancellationToken(cancellationToken).SendAsync(request.Message)
            : (await new Proto.Chat.ChatClient(channel).SendAsync(new Proto.SendRequest { Message = request.Message }, cancellationToken: cancellationToken)).Message;
        return KawaResult<SendReply>.Success(new SendReply(response));
    }
}
