namespace Kawa.Abstractions;

/// <summary>
/// Describes a predictable error response for a Kawa use case.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class KawaErrorResponseAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KawaErrorResponseAttribute" /> class.
    /// </summary>
    /// <param name="kind">The Kawa error kind produced by the use case.</param>
    public KawaErrorResponseAttribute(KawaErrorKind kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// Gets the Kawa error kind produced by the use case.
    /// </summary>
    public KawaErrorKind Kind { get; }

    /// <summary>
    /// Gets or sets a short description for the error response.
    /// </summary>
    public string? Description { get; set; }
}
