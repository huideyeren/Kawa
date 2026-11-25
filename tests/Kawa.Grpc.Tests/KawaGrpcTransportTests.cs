using Grpc.Core;
using Grpc.Net.Client;
using Kawa.Abstractions;
using Kawa.Grpc.BuildFixture;
using Kawa.Grpc.BuildFixture.Generated;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kawa.Grpc.Tests;

/// <summary>
/// Verifies generated Kawa-originated adapters over an actual standard gRPC connection.
/// </summary>
public sealed class KawaGrpcTransportTests
{
    /// <summary>
    /// Ensures the generated adapter converts protobuf messages, executes the Kawa use case, and converts the response.
    /// </summary>
    [Fact]
    public async Task GeneratedUnaryAdapter_ExecutesUseCaseOverGrpc()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IUseCase<Kawa.Grpc.BuildFixture.SendRequest, Kawa.Grpc.BuildFixture.SendResponse>, SendUseCase>());
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = new Kawa.Grpc.BuildFixture.Proto.Chat.ChatClient(channel);

        var response = await client.SendAsync(new Kawa.Grpc.BuildFixture.Proto.SendRequest { Message = "Tenryu" });

        Assert.True(response.Accepted);
    }

    /// <summary>
    /// Ensures predictable Kawa failures terminate the RPC with the mapped gRPC status and portable trailer.
    /// </summary>
    [Fact]
    public async Task GeneratedUnaryAdapter_MapsKawaErrorToGrpcTrailer()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IUseCase<Kawa.Grpc.BuildFixture.SendRequest, Kawa.Grpc.BuildFixture.SendResponse>, SendUseCase>());
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = new Kawa.Grpc.BuildFixture.Proto.Chat.ChatClient(channel);

        var exception = await Assert.ThrowsAsync<RpcException>(() => client.SendAsync(new Kawa.Grpc.BuildFixture.Proto.SendRequest { Message = "other" }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Contains("Validation", System.Text.Encoding.UTF8.GetString(exception.Trailers.Single(entry => entry.Key == "kawa-error-bin").ValueBytes));
    }

    /// <summary>
    /// Ensures a Kawa server-streaming use case is exposed as a standard gRPC response stream.
    /// </summary>
    [Fact]
    public async Task GeneratedServerStreamingAdapter_StreamsUseCaseResponsesOverGrpc()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IServerStreamingUseCase<WatchRequest, WatchResponse>, WatchUseCase>());
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = new Kawa.Grpc.BuildFixture.Proto.Chat.ChatClient(channel);

        using var call = client.Watch(new Kawa.Grpc.BuildFixture.Proto.WatchRequest { Message = "Tenryu" });
        var messages = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            messages.Add(call.ResponseStream.Current.Message);
        }

        Assert.Equal(["Tenryu-1", "Tenryu-2"], messages);
    }

    /// <summary>
    /// Ensures a Kawa client-streaming use case consumes a standard gRPC request stream.
    /// </summary>
    [Fact]
    public async Task GeneratedClientStreamingAdapter_ConsumesRequestsOverGrpc()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IClientStreamingUseCase<SumRequest, SumResponse>, SumUseCase>());
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = new Kawa.Grpc.BuildFixture.Proto.Chat.ChatClient(channel);

        using var call = client.Sum();
        await call.RequestStream.WriteAsync(new Kawa.Grpc.BuildFixture.Proto.SumRequest { Value = 4 });
        await call.RequestStream.WriteAsync(new Kawa.Grpc.BuildFixture.Proto.SumRequest { Value = 6 });
        await call.RequestStream.CompleteAsync();

        Assert.Equal(10, (await call.ResponseAsync).Total);
    }

    /// <summary>
    /// Ensures a Kawa duplex-streaming use case converts messages in both directions over gRPC.
    /// </summary>
    [Fact]
    public async Task GeneratedDuplexStreamingAdapter_ExchangesMessagesOverGrpc()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IDuplexStreamingUseCase<EchoRequest, EchoResponse>, EchoStreamUseCase>());
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = new Kawa.Grpc.BuildFixture.Proto.Chat.ChatClient(channel);

        using var call = client.EchoStream();
        await call.RequestStream.WriteAsync(new Kawa.Grpc.BuildFixture.Proto.EchoRequest { Message = "first" });
        await call.RequestStream.WriteAsync(new Kawa.Grpc.BuildFixture.Proto.EchoRequest { Message = "second" });
        await call.RequestStream.CompleteAsync();
        var messages = new List<string>();
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            messages.Add(call.ResponseStream.Current.Message);
        }

        Assert.Equal(["first", "second"], messages);
    }

    /// <summary>Shared DTOs must not redirect an operation to another implementation.</summary>
    [Fact]
    public async Task SharedDtoOperations_ResolveTheirOwnImplementations()
    {
        using var host = await CreateHostAsync(_ => { });
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = new Kawa.Grpc.BuildFixture.Proto.Chat.ChatClient(channel);
        Assert.True((await client.SendAsync(new Kawa.Grpc.BuildFixture.Proto.SendRequest { Message = "Tenryu" })).Accepted);
        Assert.False((await client.RejectAsync(new Kawa.Grpc.BuildFixture.Proto.SendRequest { Message = "Tenryu" })).Accepted);
    }

    /// <summary>Bytes, nested messages, enums and repeated fields survive a real RPC round trip.</summary>
    [Fact]
    public async Task CompositeContract_RoundTripsEveryField()
    {
        using var host = await CreateHostAsync(services => services.AddScoped<RoundTripUseCase>());
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = new Kawa.Grpc.BuildFixture.Proto.Composite.CompositeClient(channel);
        var request = new Kawa.Grpc.BuildFixture.Proto.Envelope
        {
            Message = "all fields", Payload = Google.Protobuf.ByteString.CopyFrom(new byte[] { 0, 128, 255 }),
            State = (Kawa.Grpc.BuildFixture.Proto.Delivery)3,
            Child = new Kawa.Grpc.BuildFixture.Proto.Envelope_Detail { Name = "nested" },
        };
        request.Items.Add(new Kawa.Grpc.BuildFixture.Proto.Envelope_Detail { Name = "repeated" });
        request.Scores.Add(new[] { 1, 2, 3 });
        request.States.Add((Kawa.Grpc.BuildFixture.Proto.Delivery)3);
        var response = await client.RoundTripAsync(request);
        Assert.Equal(request, response);
    }

    private static Task<IHost> CreateHostAsync(Action<IServiceCollection> configure) => new HostBuilder()
        .ConfigureWebHost(webHost => webHost
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddKawaGrpc();
                // One generated adapter owns the complete protobuf service and therefore resolves every operation.
                // Individual tests can replace the operation they exercise by registering the same contract below.
                services.AddSingleton<IUseCase<SendRequest, SendResponse>, SendUseCase>();
                services.AddSingleton<IServerStreamingUseCase<WatchRequest, WatchResponse>, WatchUseCase>();
                services.AddSingleton<IClientStreamingUseCase<SumRequest, SumResponse>, SumUseCase>();
                services.AddSingleton<IDuplexStreamingUseCase<EchoRequest, EchoResponse>, EchoStreamUseCase>();
                services.AddSingleton<IUseCase<SendRequest, SendResponse>, RejectUseCase>();
                configure(services);
            })
            .Configure(app => app.UseRouting().UseEndpoints(endpoints =>
            {
                // gRPC registers every RPC in a generated base type, so one adapter owns each protobuf service.
                endpoints.MapGrpcService<ChatGrpcAdapter>();
                endpoints.MapGrpcService<CompositeGrpcAdapter>();
            })))
        .StartAsync();
}
