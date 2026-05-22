namespace Kawa.Sample.Mixed.UseCases

/// <summary>Carries the mixed sample create-user response.</summary>
/// <param name="id">The generated sample user identifier.</param>
/// <param name="name">The created user name.</param>
type CreateUserResponse(id: string, name: string) =
    /// <summary>Gets the generated sample user identifier.</summary>
    member _.Id = id

    /// <summary>Gets the created user name.</summary>
    member _.Name = name
