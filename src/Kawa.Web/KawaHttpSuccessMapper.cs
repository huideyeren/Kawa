using Kawa.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kawa.Web;

/// <summary>
/// Maps successful Kawa response contracts to ASP.NET Core HTTP results.
/// </summary>
public sealed class KawaHttpSuccessMapper : IResultMapper<IResult>
{
    /// <inheritdoc />
    public IResult MapSuccess<TResponse>(TResponse response)
    {
        return Results.Ok(response);
    }
}
