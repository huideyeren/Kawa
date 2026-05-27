Imports System
Imports System.Threading
Imports System.Threading.Tasks
Imports Kawa.Abstractions

Namespace Kawa.Sample.VB.UseCases
    ''' <summary>Creates a simple user response from the VB.NET sample use case.</summary>
    Public NotInheritable Class CreateUserUseCase
        Implements IUseCase(Of CreateUserRequest, CreateUserResponse)

        ''' <inheritdoc />
        Public Function ExecuteAsync(
            request As CreateUserRequest,
            Optional cancellationToken As CancellationToken = Nothing) As Task(Of KawaResult(Of CreateUserResponse)) _
            Implements IUseCase(Of CreateUserRequest, CreateUserResponse).ExecuteAsync

            cancellationToken.ThrowIfCancellationRequested()

            Dim response = New CreateUserResponse(Guid.NewGuid().ToString("N"), request.Name)
            Return Task.FromResult(KawaResult(Of CreateUserResponse).Success(response))
        End Function
    End Class
End Namespace
