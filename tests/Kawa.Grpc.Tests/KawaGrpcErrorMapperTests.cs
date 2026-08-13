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
}
