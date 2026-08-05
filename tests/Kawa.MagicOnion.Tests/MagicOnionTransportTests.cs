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

    [KawaMagicOnionService("echo")]
    public sealed class EchoUseCase : IUseCase<string, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default)
            => Task.FromResult(KawaResult<string>.Success(request));
    }
}
