using Kawa.Abstractions;

namespace Kawa.Core.Tests;

/// <summary>
/// Verifies the public transport-independent streaming contract surface.
/// </summary>
public sealed class StreamingContractTests
{
    /// <summary>
    /// Verifies that the error model has the transport-neutral categories
    /// required to represent cancellation, deadlines, and temporary failures.
    /// </summary>
    [Theory]
    [InlineData("Cancelled")]
    [InlineData("DeadlineExceeded")]
    [InlineData("AlreadyExists")]
    [InlineData("PreconditionFailed")]
    [InlineData("ResourceExhausted")]
    [InlineData("Unavailable")]
    public void KawaErrorKind_ContainsTransportNeutralCategory(string name)
    {
        Assert.True(Enum.TryParse<KawaErrorKind>(name, out var kind));
        Assert.Equal(name, kind.ToString());
    }

    /// <summary>
    /// Verifies that the stream contracts remain available without a transport dependency.
    /// </summary>
    [Fact]
    public void StreamingContracts_ArePublicTransportIndependentInterfaces()
    {
        Assert.True(typeof(IServerStreamingUseCase<int, string>).IsInterface);
        Assert.True(typeof(IClientStreamingUseCase<int, string>).IsInterface);
        Assert.True(typeof(IDuplexStreamingUseCase<int, string>).IsInterface);
        Assert.True(typeof(IStreamingHubUseCase<string, string>).IsInterface);
        Assert.True(typeof(IKawaHubSession<string>).IsInterface);
    }
}
