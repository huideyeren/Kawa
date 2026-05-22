using Kawa.Abstractions;

namespace Kawa.Core.Tests;

/// <summary>
/// Verifies the Kawa result model.
/// </summary>
public class KawaResultTests
{
    /// <summary>
    /// Verifies that a success result carries its value.
    /// </summary>
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = KawaResult<string>.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("ok", result.Value);
        Assert.Null(result.Error);
    }

    /// <summary>
    /// Verifies that a failure result carries its error.
    /// </summary>
    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var error = new KawaError(KawaErrorKind.NotFound, "Missing");

        var result = KawaResult<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Equal(error, result.Error);
    }
}
