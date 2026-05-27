namespace Kawa.Abstractions;

/// <summary>
/// Maps Kawa results to transport-specific results.
/// </summary>
/// <typeparam name="TTransportResult">The result type used by the transport.</typeparam>
public interface ITransportMapper<out TTransportResult>
{
    /// <summary>
    /// Maps a Kawa result to a transport-specific result.
    /// </summary>
    /// <typeparam name="TResponse">The response contract type.</typeparam>
    /// <param name="result">The Kawa result to map.</param>
    /// <returns>The transport-specific result.</returns>
    TTransportResult Map<TResponse>(KawaResult<TResponse> result);
}
