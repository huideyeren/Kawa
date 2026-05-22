namespace Kawa.FSharp

open Kawa.Abstractions

[<RequireQualifiedAccess>]
/// <summary>Creates Kawa results from F# code.</summary>
module KawaResult =
    /// <summary>Creates a successful Kawa result.</summary>
    /// <param name="value">The successful value.</param>
    let success value =
        KawaResult<_>.Success(value)

    /// <summary>Creates a failed Kawa result.</summary>
    /// <param name="kind">The application error category.</param>
    /// <param name="message">The application error message.</param>
    let failure<'T> kind message =
        KawaResult<'T>.Failure(KawaError(kind, message))
