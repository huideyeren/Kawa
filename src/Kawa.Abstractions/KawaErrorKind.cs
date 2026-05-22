namespace Kawa.Abstractions;

/// <summary>
/// Identifies the category of a Kawa application error.
/// </summary>
public enum KawaErrorKind
{
    /// <summary>
    /// The request failed validation.
    /// </summary>
    Validation,

    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The caller is not authenticated.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// The caller is not allowed to perform the operation.
    /// </summary>
    Forbidden,

    /// <summary>
    /// The operation conflicts with current application state.
    /// </summary>
    Conflict,

    /// <summary>
    /// The failure has no more specific application category.
    /// </summary>
    Unknown,
}
