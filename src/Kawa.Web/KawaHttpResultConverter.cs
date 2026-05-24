using Kawa.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kawa.Web;

/// <summary>
/// Converts Kawa results into ASP.NET Core HTTP results.
/// </summary>
public static class KawaHttpResultConverter
{
    /// <summary>
    /// Converts a Kawa result to an ASP.NET Core HTTP result.
    /// </summary>
    /// <typeparam name="TResponse">The successful response contract type.</typeparam>
    /// <param name="result">The Kawa result to convert.</param>
    /// <returns>An HTTP result for the supplied Kawa result.</returns>
    public static IResult ToIResult<TResponse>(KawaResult<TResponse> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var error = result.Error!;

        return error.Kind switch
        {
            KawaErrorKind.Validation => Results.BadRequest(error),
            KawaErrorKind.Unauthorized => Results.StatusCode(StatusCodes.Status401Unauthorized),
            KawaErrorKind.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
            KawaErrorKind.NotFound => Results.NotFound(error),
            KawaErrorKind.Conflict => Results.Conflict(error),
            KawaErrorKind.Unknown => Results.Problem(error.Message),
            _ => Results.Problem(error.Message),
        };
    }
}
