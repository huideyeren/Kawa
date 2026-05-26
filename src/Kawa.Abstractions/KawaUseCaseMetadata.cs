namespace Kawa.Abstractions;

/// <summary>
/// Describes a Kawa use case for API catalogs and transport metadata.
/// </summary>
/// <param name="Name">The stable use case name.</param>
/// <param name="Summary">The short use case summary.</param>
/// <param name="Description">The longer use case description.</param>
/// <param name="Version">The use case contract version.</param>
/// <param name="Tags">The catalog tags associated with the use case.</param>
public sealed record KawaUseCaseMetadata(
    string Name,
    string? Summary,
    string? Description,
    string Version,
    IReadOnlyList<string> Tags);
