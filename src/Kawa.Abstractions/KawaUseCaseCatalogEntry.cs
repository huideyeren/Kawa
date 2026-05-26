namespace Kawa.Abstractions;

/// <summary>
/// Describes a Kawa use case and its request, response, and error contracts.
/// </summary>
/// <param name="UseCaseType">The use case implementation type.</param>
/// <param name="RequestType">The request contract type.</param>
/// <param name="ResponseType">The response contract type.</param>
/// <param name="Metadata">The use case catalog metadata.</param>
/// <param name="ErrorResponses">The predictable error responses for the use case.</param>
public sealed record KawaUseCaseCatalogEntry(
    Type UseCaseType,
    Type RequestType,
    Type ResponseType,
    KawaUseCaseMetadata Metadata,
    IReadOnlyList<KawaErrorResponseMetadata> ErrorResponses);
