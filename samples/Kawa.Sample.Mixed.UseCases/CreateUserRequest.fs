namespace Kawa.Sample.Mixed.UseCases

[<AllowNullLiteral>]
/// <summary>Carries the name for the mixed sample create-user request.</summary>
type CreateUserRequest() =
    /// <summary>Gets or sets the user name to create.</summary>
    member val Name = "" with get, set
