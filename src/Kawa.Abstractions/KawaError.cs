namespace Kawa.Abstractions;

/// <summary>
/// Describes a predictable application error.
/// </summary>
/// <param name="Kind">The category of the error.</param>
/// <param name="Message">The human-readable error message.</param>
public sealed record KawaError(KawaErrorKind Kind, string Message)
{
    /// <inheritdoc />
    public override string ToString() => $"{Kind}: {Message}";
}
