namespace Kawa.Sample.Mixed.UseCases

open System
open System.Threading
open System.Threading.Tasks
open Kawa.Abstractions
open Kawa.FSharp

/// <summary>Creates a simple user response from the mixed sample F# use case.</summary>
type CreateUserUseCase() =
    interface IUseCase<CreateUserRequest, CreateUserResponse> with
        /// <inheritdoc />
        member _.ExecuteAsync(request: CreateUserRequest, cancellationToken: CancellationToken) =
            cancellationToken.ThrowIfCancellationRequested()

            CreateUserResponse(Guid.NewGuid().ToString("N"), request.Name)
            |> KawaResult.success
            |> Task.FromResult
