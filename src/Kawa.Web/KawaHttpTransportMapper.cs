using Kawa.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kawa.Web;

/// <summary>
/// Maps Kawa results to ASP.NET Core HTTP results.
/// </summary>
public sealed class KawaHttpTransportMapper : ITransportMapper<IResult>
{
    private readonly IResultMapper<IResult> resultMapper;
    private readonly IErrorMapper<IResult> errorMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="KawaHttpTransportMapper" /> class.
    /// </summary>
    /// <param name="resultMapper">The mapper used for successful responses.</param>
    /// <param name="errorMapper">The mapper used for failed responses.</param>
    public KawaHttpTransportMapper(
        IResultMapper<IResult> resultMapper,
        IErrorMapper<IResult> errorMapper)
    {
        this.resultMapper = resultMapper;
        this.errorMapper = errorMapper;
    }

    /// <inheritdoc />
    public IResult Map<TResponse>(KawaResult<TResponse> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? resultMapper.MapSuccess(result.Value!)
            : errorMapper.MapError(result.Error!);
    }
}
