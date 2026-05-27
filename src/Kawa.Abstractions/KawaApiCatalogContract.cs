namespace Kawa.Abstractions;

/// <summary>
/// Describes a request or response contract in the public API catalog.
/// </summary>
/// <param name="Name">The contract type name.</param>
/// <param name="FullName">The contract full type name.</param>
public sealed record KawaApiCatalogContract(string Name, string? FullName)
{
    /// <summary>
    /// Creates contract metadata from a CLR type.
    /// </summary>
    /// <param name="type">The contract type.</param>
    /// <returns>The generated contract metadata.</returns>
    public static KawaApiCatalogContract FromType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return new KawaApiCatalogContract(type.Name, type.FullName);
    }
}
