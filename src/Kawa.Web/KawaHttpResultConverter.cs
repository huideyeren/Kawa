using Kawa.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kawa.Web;

/// <summary>
/// Converts Kawa results into ASP.NET Core HTTP results.
/// </summary>
public static class KawaHttpResultConverter
{
    private static readonly KawaHttpTransportMapper DefaultMapper = new(
        new KawaHttpSuccessMapper(),
        new KawaHttpErrorMapper());

    /// <summary>
    /// Converts a Kawa result to an ASP.NET Core HTTP result.
    /// </summary>
    /// <typeparam name="TResponse">The successful response contract type.</typeparam>
    /// <param name="result">The Kawa result to convert.</param>
    /// <returns>An HTTP result for the supplied Kawa result.</returns>
    public static IResult ToIResult<TResponse>(KawaResult<TResponse> result)
    {
        return DefaultMapper.Map(result);
    }
}
