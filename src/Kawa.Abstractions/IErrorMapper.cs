namespace Kawa.Abstractions;

/// <summary>
/// Maps Kawa errors to transport-specific error results.
/// </summary>
/// <typeparam name="TTransportResult">The result type used by the transport.</typeparam>
public interface IErrorMapper<out TTransportResult>
{
    /// <summary>
    /// Maps a Kawa error to a transport-specific result.
    /// </summary>
    /// <param name="error">The Kawa error to map.</param>
    /// <returns>The transport-specific result.</returns>
    TTransportResult MapError(KawaError error);
}
