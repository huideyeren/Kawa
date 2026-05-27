namespace Kawa.Sample.FSharp.UseCases

open System
open System.Threading
open System.Threading.Tasks
open Kawa.Abstractions
open Kawa.FSharp

[<AllowNullLiteral>]
/// <summary>Carries the name for the F# sample create-user request.</summary>
type CreateUserRequest() =
    /// <summary>Gets or sets the user name to create.</summary>
    member val Name = "" with get, set

/// <summary>Carries the F# sample create-user response.</summary>
/// <param name="id">The generated sample user identifier.</param>
/// <param name="name">The created user name.</param>
type CreateUserResponse(id: string, name: string) =
    /// <summary>Gets the generated sample user identifier.</summary>
    member _.Id = id

    /// <summary>Gets the created user name.</summary>
    member _.Name = name

/// <summary>Creates a simple user response from the F# sample use case.</summary>
type CreateUserUseCase() =
    interface IUseCase<CreateUserRequest, CreateUserResponse> with
        /// <inheritdoc />
        member _.ExecuteAsync(request: CreateUserRequest, cancellationToken: CancellationToken) =
            cancellationToken.ThrowIfCancellationRequested()

            CreateUserResponse(Guid.NewGuid().ToString("N"), request.Name)
            |> KawaResult.success
            |> Task.FromResult
