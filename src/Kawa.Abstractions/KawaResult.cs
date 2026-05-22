namespace Kawa.Abstractions;

/// <summary>
/// Represents the success or failure result of a Kawa operation.
/// </summary>
/// <typeparam name="T">The successful value type.</typeparam>
public sealed record KawaResult<T>
{
    private KawaResult(T? value, KawaError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Gets the successful value when the result succeeded.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the application error when the result failed.
    /// </summary>
    public KawaError? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether the result succeeded.
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Gets a value indicating whether the result failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The successful value.</param>
    /// <returns>A successful result containing <paramref name="value" />.</returns>
    public static KawaResult<T> Success(T value) => new(value, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The application error.</param>
    /// <returns>A failed result containing <paramref name="error" />.</returns>
    public static KawaResult<T> Failure(KawaError error) => new(default, error);
}
