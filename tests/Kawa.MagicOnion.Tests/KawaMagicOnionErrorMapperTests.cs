using System.Text;
using Grpc.Core;
using Kawa.Abstractions;
using Xunit;

namespace Kawa.MagicOnion.Tests;

/// <summary>
/// Verifies conversion from predictable Kawa errors to gRPC transport errors.
/// </summary>
public sealed class KawaMagicOnionErrorMapperTests
{
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
