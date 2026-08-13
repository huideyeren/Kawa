using Kawa.Abstractions;
using Kawa.MagicOnion;
using MagicOnion.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kawa.MagicOnion.Tests;

public sealed class MagicOnionTransportTests
{
    [Fact]
    public async Task GeneratedUnaryAdapter_ExecutesUseCaseOverMagicOnion()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddKawaMagicOnion();
                    services.AddSingleton<IUseCase<string, string>, EchoUseCase>();
                })
                .Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapKawaMagicOnionServices())))
            .StartAsync();

        using var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost", new Grpc.Net.Client.GrpcChannelOptions
        {
            HttpHandler = host.GetTestServer().CreateHandler(),
        });
        var client = MagicOnionClient.Create<Generated.IEchoMagicOnionService>(channel);

        var result = await client.EchoAsync("Tenryu");

        Assert.Equal("Tenryu", result);
    }

    [Fact]
    public async Task GeneratedServerStreamingAdapter_StreamsUseCaseResponses()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IServerStreamingUseCase<string, string>, WatchUseCase>());
        using var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost", new Grpc.Net.Client.GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = MagicOnionClient.Create<Generated.IWatchMagicOnionService>(channel);

        var stream = await client.WatchAsync("Tenryu");
        var values = new List<string>();
        while (await stream.ResponseStream.MoveNext(CancellationToken.None)) values.Add(stream.ResponseStream.Current);

        Assert.Equal(["Tenryu-1", "Tenryu-2"], values);
    }

    [Fact]
    public async Task GeneratedClientStreamingAdapter_ConsumesAllRequests()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IClientStreamingUseCase<int, int>, SumUseCase>());
        using var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost", new Grpc.Net.Client.GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = MagicOnionClient.Create<Generated.ISumMagicOnionService>(channel);
        var stream = await client.SumAsync();
        await stream.RequestStream.WriteAsync(1);
        await stream.RequestStream.WriteAsync(2);
        await stream.RequestStream.CompleteAsync();
        Assert.Equal(3, await stream.ResponseAsync);
    }

    [Fact]
    public async Task GeneratedDuplexStreamingAdapter_EchoesRequests()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IDuplexStreamingUseCase<string, string>, EchoStreamUseCase>());
        using var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost", new Grpc.Net.Client.GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var client = MagicOnionClient.Create<Generated.IEchoStreamMagicOnionService>(channel);
        var stream = await client.EchoStreamAsync();
        await stream.RequestStream.WriteAsync("Tenryu");
        await stream.RequestStream.CompleteAsync();
        Assert.True(await stream.ResponseStream.MoveNext(CancellationToken.None));
        Assert.Equal("Tenryu", stream.ResponseStream.Current);
    }

    [Fact]
    public async Task GeneratedStreamingHubAdapter_BroadcastsTypedEventsToJoinedClients()
    {
        using var host = await CreateHostAsync(services => services.AddSingleton<IStreamingHubUseCase<string, string>, ChatHubUseCase>());
        using var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost", new Grpc.Net.Client.GrpcChannelOptions { HttpHandler = host.GetTestServer().CreateHandler() });
        var firstReceiver = new ChatHubReceiver();
        var secondReceiver = new ChatHubReceiver();
        await using var first = await StreamingHubClient.ConnectAsync<Generated.IChatMagicOnionHub, Generated.IChatMagicOnionHubReceiver>(channel, firstReceiver);
        await using var second = await StreamingHubClient.ConnectAsync<Generated.IChatMagicOnionHub, Generated.IChatMagicOnionHubReceiver>(channel, secondReceiver);

        await first.HandleAsync("Tenryu");

        Assert.Equal("Tenryu", await firstReceiver.Event.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("Tenryu", await secondReceiver.Event.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [KawaMagicOnionService("echo")]
    public sealed class EchoUseCase : IUseCase<string, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default)
            => Task.FromResult(KawaResult<string>.Success(request));
    }

    [KawaMagicOnionService("watch")]
    public sealed class WatchUseCase : IServerStreamingUseCase<string, string>
    {
        public async IAsyncEnumerable<KawaResult<string>> ExecuteAsync(string request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return KawaResult<string>.Success(request + "-1");
            yield return KawaResult<string>.Success(request + "-2");
            await Task.CompletedTask;
        }
    }

    [KawaMagicOnionService("sum")]
    public sealed class SumUseCase : IClientStreamingUseCase<int, int>
    {
        public async Task<KawaResult<int>> ExecuteAsync(IAsyncEnumerable<int> requests, CancellationToken cancellationToken = default)
        { var sum = 0; await foreach (var value in requests) sum += value; return KawaResult<int>.Success(sum); }
    }

    [KawaMagicOnionService("echo-stream")]
    public sealed class EchoStreamUseCase : IDuplexStreamingUseCase<string, string>
    {
        public async IAsyncEnumerable<KawaResult<string>> ExecuteAsync(IAsyncEnumerable<string> requests, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await foreach (var value in requests) yield return KawaResult<string>.Success(value); }
    }

    [KawaMagicOnionHub("chat")]
    public sealed class ChatHubUseCase : IStreamingHubUseCase<string, string>
    {
        public Task OnConnectedAsync(IKawaHubSession<string> session, CancellationToken cancellationToken = default) => session.JoinGroupAsync("tenryu", cancellationToken);
        public Task HandleAsync(string clientEvent, IKawaHubSession<string> session, CancellationToken cancellationToken = default) => session.BroadcastToGroupAsync("tenryu", clientEvent, cancellationToken);
        public Task OnDisconnectedAsync(IKawaHubSession<string> session, Exception? exception, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ChatHubReceiver : Generated.IChatMagicOnionHubReceiver
    {
        public TaskCompletionSource<string> Event { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void OnEvent(string serverEvent) => Event.TrySetResult(serverEvent);
    }

    private static Task<IHost> CreateHostAsync(Action<IServiceCollection> configure) => new HostBuilder().ConfigureWebHost(webHost => webHost.UseTestServer().ConfigureServices(services => { services.AddKawaMagicOnion(); configure(services); }).Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapKawaMagicOnionServices()))).StartAsync();
}
