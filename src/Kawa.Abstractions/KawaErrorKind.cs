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
    /// The caller cancelled the operation.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The operation did not finish before its deadline.
    /// </summary>
    DeadlineExceeded,

    /// <summary>
    /// The target being created already exists.
    /// </summary>
    AlreadyExists,

    /// <summary>
    /// The current state does not satisfy an operation precondition.
    /// </summary>
    PreconditionFailed,

    /// <summary>
    /// A quota, capacity limit, or rate limit was exhausted.
    /// </summary>
    ResourceExhausted,

    /// <summary>
    /// A temporary failure makes retrying the operation meaningful.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The failure has no more specific application category.
    /// </summary>
    Unknown,
}
