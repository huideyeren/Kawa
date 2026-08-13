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

    private static Task<IHost> CreateHostAsync(Action<IServiceCollection> configure) => new HostBuilder()
        .ConfigureWebHost(webHost => webHost
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddKawaGrpc();
                configure(services);
            })
            .Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapGrpcService<SendGrpcAdapter>())))
        .StartAsync();
}
