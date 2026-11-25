namespace Kawa.Abstractions;

/// <summary>
/// Identifies the category of a Kawa application error.
/// </summary>
public enum KawaErrorKind
{
    /// <summary>
    /// The request failed validation.
    /// </summary>
    Validation = 0,

    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// The caller is not authenticated.
    /// </summary>
    Unauthorized = 2,

    /// <summary>
    /// The caller is not allowed to perform the operation.
    /// </summary>
    Forbidden = 3,

    /// <summary>
    /// The operation conflicts with current application state.
    /// </summary>
    Conflict = 4,

    /// <summary>
    /// The caller cancelled the operation.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// The operation did not finish before its deadline.
    /// </summary>
    DeadlineExceeded = 7,

    /// <summary>
    /// The target being created already exists.
    /// </summary>
    AlreadyExists = 8,

    /// <summary>
    /// The current state does not satisfy an operation precondition.
    /// </summary>
    PreconditionFailed = 9,

    /// <summary>
    /// A quota, capacity limit, or rate limit was exhausted.
    /// </summary>
    ResourceExhausted = 10,

    /// <summary>
    /// A temporary failure makes retrying the operation meaningful.
    /// </summary>
    Unavailable = 11,

    /// <summary>
    /// The failure has no more specific application category.
    /// </summary>
    Unknown = 5,
}
