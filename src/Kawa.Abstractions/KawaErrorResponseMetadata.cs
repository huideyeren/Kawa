namespace Kawa.Abstractions;

/// <summary>
/// Describes a predictable error response for API catalogs and transports.
/// </summary>
/// <param name="Kind">The Kawa error kind.</param>
/// <param name="Description">The error description.</param>
public sealed record KawaErrorResponseMetadata(KawaErrorKind Kind, string Description);
