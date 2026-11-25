using System.Text;
using Grpc.Core;
using Kawa.Abstractions;
using Kawa.Grpc;
using Xunit;

namespace Kawa.Grpc.Tests;

/// <summary>
/// Verifies the standard gRPC representation of predictable Kawa errors.
/// </summary>
public sealed class KawaGrpcErrorMapperTests
{
    [Theory]
    [InlineData(KawaErrorKind.Unknown)]
    [InlineData((KawaErrorKind)999)]
    public void InternalErrors_RedactStatusAndTrailer(KawaErrorKind kind)
    {
        var error = new KawaError(kind, "secret-connection-string");
        var mapped = KawaGrpcErrorMapper.Map(error);
        Assert.Equal(StatusCode.Internal, mapped.StatusCode);
        Assert.Equal("An unexpected error occurred.", mapped.Detail);
        using var json = System.Text.Json.JsonDocument.Parse(mapped.KawaErrorJson);
        Assert.Equal("Unknown", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal(mapped.Detail, json.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain(error.Message, Encoding.UTF8.GetString(mapped.KawaErrorJson));
        Assert.Equal("secret-connection-string", error.Message);
    }

    /// <summary>
    /// Ensures Kawa's retryable failure keeps the portable status and structured error payload.
    /// </summary>
    [Fact]
    public void Map_MapsUnavailableErrorToStatusAndTrailer()
    {
        var error = new KawaError(KawaErrorKind.Unavailable, "Database is restarting.");

        var mapped = KawaGrpcErrorMapper.Map(error);

        Assert.Equal(StatusCode.Unavailable, mapped.StatusCode);
        Assert.Equal(error.Message, mapped.Detail);
        Assert.Contains("Unavailable", Encoding.UTF8.GetString(mapped.KawaErrorJson));
    }

    /// <summary>
    /// Ensures a mapped error can terminate a standard gRPC call with its portable trailer.
    /// </summary>
    [Fact]
    public void ToRpcException_PreservesStatusAndKawaTrailer()
    {
        var exception = KawaGrpcErrorMapper.ToRpcException(new KawaError(KawaErrorKind.Validation, "Message is required."));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        var trailer = exception.Trailers.Single(entry => entry.Key == "kawa-error-bin");
        Assert.Contains("Validation", Encoding.UTF8.GetString(trailer.ValueBytes));
    }
}
