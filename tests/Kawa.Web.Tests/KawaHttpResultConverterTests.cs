using System.Collections.Generic;
using Kawa.Abstractions;
using Kawa.Web;
using Microsoft.AspNetCore.Http;

namespace Kawa.Web.Tests;

/// <summary>
/// Verifies HTTP conversion for Kawa results.
/// </summary>
public class KawaHttpResultConverterTests
{
    /// <summary>
    /// Verifies that successful results become HTTP 200 results.
    /// </summary>
    [Fact]
    public void ToIResult_ReturnsOkForSuccess()
    {
        var result = KawaResult<string>.Success("created");

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(
            KawaHttpResultConverter.ToIResult(result));

        Assert.Equal(StatusCodes.Status200OK, httpResult.StatusCode);
    }

    /// <summary>
    /// Verifies that Kawa error kinds map to the expected HTTP status code.
    /// </summary>
    /// <param name="kind">The Kawa error category.</param>
    /// <param name="expectedStatusCode">The expected HTTP status code.</param>
    [Theory]
    [MemberData(nameof(FailureStatusCodes))]
    public void ToIResult_MapsFailureKindToHttpStatusCode(
        KawaErrorKind kind,
        int expectedStatusCode)
    {
        var result = KawaResult<string>.Failure(new KawaError(kind, "Request failed"));

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(
            KawaHttpResultConverter.ToIResult(result));

        Assert.Equal(expectedStatusCode, httpResult.StatusCode);
    }

    /// <summary>
    /// Verifies that a null result is rejected.
    /// </summary>
    [Fact]
    public void ToIResult_ThrowsForNullResult()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => KawaHttpResultConverter.ToIResult<string>(null!));

        Assert.Equal("result", exception.ParamName);
    }

    /// <summary>
    /// Verifies that an unknown enum value becomes a problem response.
    /// </summary>
    [Fact]
    public void ToIResult_ReturnsProblemForUnexpectedFailureKind()
    {
        var result = KawaResult<string>.Failure(new KawaError((KawaErrorKind)999, "Unexpected"));

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(
            KawaHttpResultConverter.ToIResult(result));

        Assert.Equal(StatusCodes.Status500InternalServerError, httpResult.StatusCode);
    }

    /// <summary>
    /// Gets the expected HTTP status codes for failed Kawa results.
    /// </summary>
    /// <returns>The Kawa error categories and expected HTTP status codes.</returns>
    public static IEnumerable<object[]> FailureStatusCodes()
    {
        yield return [KawaErrorKind.Validation, StatusCodes.Status400BadRequest];
        yield return [KawaErrorKind.Unauthorized, StatusCodes.Status401Unauthorized];
        yield return [KawaErrorKind.Forbidden, StatusCodes.Status403Forbidden];
        yield return [KawaErrorKind.NotFound, StatusCodes.Status404NotFound];
        yield return [KawaErrorKind.Conflict, StatusCodes.Status409Conflict];
        yield return [KawaErrorKind.Unknown, StatusCodes.Status500InternalServerError];
    }
}
