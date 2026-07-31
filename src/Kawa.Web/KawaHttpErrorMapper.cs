using Kawa.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kawa.Web;

/// <summary>
/// Maps Kawa errors to ASP.NET Core HTTP results.
/// </summary>
public sealed class KawaHttpErrorMapper : IErrorMapper<IResult>
{
    /// <inheritdoc />
    public IResult MapError(KawaError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Kind switch
        {
            KawaErrorKind.Validation => Results.BadRequest(error),
            KawaErrorKind.Unauthorized => Results.StatusCode(StatusCodes.Status401Unauthorized),
            KawaErrorKind.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
            KawaErrorKind.NotFound => Results.NotFound(error),
            KawaErrorKind.Conflict => Results.Conflict(error),
            KawaErrorKind.Cancelled => Results.StatusCode(499),
            KawaErrorKind.DeadlineExceeded => Results.StatusCode(StatusCodes.Status504GatewayTimeout),
            KawaErrorKind.AlreadyExists => Results.Conflict(error),
            KawaErrorKind.PreconditionFailed => Results.StatusCode(StatusCodes.Status412PreconditionFailed),
            KawaErrorKind.ResourceExhausted => Results.StatusCode(StatusCodes.Status429TooManyRequests),
            KawaErrorKind.Unavailable => Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
            KawaErrorKind.Unknown => Results.Problem(error.Message),
            _ => Results.Problem(error.Message),
        };
    }
}
