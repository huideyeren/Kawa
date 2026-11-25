using System.Text;
using Grpc.Core;
using Kawa.Abstractions;
using Kawa.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kawa.MagicOnion.Tests;

/// <summary>
/// Verifies conversion from predictable Kawa errors to gRPC transport errors.
/// </summary>
public sealed class KawaMagicOnionErrorMapperTests
{
    [Theory]
    [InlineData(KawaErrorKind.Unknown)]
    [InlineData((KawaErrorKind)999)]
    public void InternalErrors_RedactStatusAndTrailer(KawaErrorKind kind)
    {
        var error = new KawaError(kind, "secret-connection-string");
        var mapped = KawaMagicOnionErrorMapper.Map(error);
        Assert.Equal(StatusCode.Internal, mapped.StatusCode);
        Assert.Equal("An unexpected error occurred.", mapped.Detail);
        using var json = System.Text.Json.JsonDocument.Parse(mapped.KawaErrorJson);
        Assert.Equal("Unknown", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal(mapped.Detail, json.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain(error.Message, Encoding.UTF8.GetString(mapped.KawaErrorJson));
        Assert.Equal("secret-connection-string", error.Message);
    }

    /// <summary>
    /// Verifies that MagicOnion registration adds Kawa's executor services.
    /// </summary>
    [Fact]
    public void AddKawaMagicOnion_RegistersKawaExecutors()
    {
        var services = new ServiceCollection();

        services.AddKawaMagicOnion();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<UseCaseExecutor>());
        Assert.NotNull(provider.GetService<ServerStreamingUseCaseExecutor>());
        Assert.NotNull(provider.GetService<ClientStreamingUseCaseExecutor>());
        Assert.NotNull(provider.GetService<DuplexStreamingUseCaseExecutor>());
        Assert.NotNull(provider.GetService<StreamingHubUseCaseExecutor>());
    }

    /// <summary>
    /// Verifies that applications can explicitly name a generated MagicOnion service.
    /// </summary>
    [Fact]
    public void ServiceAttribute_PreservesConfiguredServiceName()
    {
        var attribute = new KawaMagicOnionServiceAttribute("chat");

        Assert.Equal("chat", attribute.Name);
    }

    /// <summary>
    /// Verifies that applications can override the generated operation name.
    /// </summary>
    [Fact]
    public void OperationAttribute_PreservesConfiguredOperationName()
    {
        var attribute = new KawaMagicOnionOperationAttribute("send");

        Assert.Equal("send", attribute.Name);
    }

    /// <summary>
    /// Verifies that applications can explicitly name a generated StreamingHub.
    /// </summary>
    [Fact]
    public void HubAttribute_PreservesConfiguredHubName()
    {
        var attribute = new KawaMagicOnionHubAttribute("chat-hub");

        Assert.Equal("chat-hub", attribute.Name);
    }

    /// <summary>
    /// Verifies that a temporary application failure becomes an unavailable gRPC status
    /// and preserves its structured Kawa error in the binary trailer.
    /// </summary>
    [Fact]
    public void Map_MapsUnavailableErrorToStatusAndTrailer()
    {
        var error = new KawaError(KawaErrorKind.Unavailable, "Database is restarting.");

        var mapped = KawaMagicOnionErrorMapper.Map(error);

        Assert.Equal(StatusCode.Unavailable, mapped.StatusCode);
        Assert.Equal(error.Message, mapped.Detail);
        Assert.Contains("Unavailable", Encoding.UTF8.GetString(mapped.KawaErrorJson));
    }

    /// <summary>
    /// Verifies that validation maps to the gRPC invalid-argument category.
    /// </summary>
    [Fact]
    public void Map_MapsValidationToInvalidArgument()
    {
        var mapped = KawaMagicOnionErrorMapper.Map(new KawaError(KawaErrorKind.Validation, "Name is required."));

        Assert.Equal(StatusCode.InvalidArgument, mapped.StatusCode);
    }
}
