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

    private static Task<IHost> CreateHostAsync(Action<IServiceCollection> configure) => new HostBuilder().ConfigureWebHost(webHost => webHost.UseTestServer().ConfigureServices(services => { services.AddKawaMagicOnion(); configure(services); }).Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapKawaMagicOnionServices()))).StartAsync();
}
