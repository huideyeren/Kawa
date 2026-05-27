namespace Kawa.Abstractions;

/// <summary>
/// Maps a successful Kawa response contract to a transport-specific result.
/// </summary>
/// <typeparam name="TTransportResult">The result type used by the transport.</typeparam>
public interface IResultMapper<out TTransportResult>
{
    /// <summary>
    /// Maps a successful response contract to a transport-specific result.
    /// </summary>
    /// <typeparam name="TResponse">The response contract type.</typeparam>
    /// <param name="response">The successful response contract.</param>
    /// <returns>The transport-specific result.</returns>
    TTransportResult MapSuccess<TResponse>(TResponse response);
}
