Imports System
Imports System.Threading
Imports System.Threading.Tasks
Imports Kawa.Abstractions

Namespace Kawa.Sample.VB.UseCases
    ''' <summary>Carries the name for the VB.NET sample create-user request.</summary>
    Public NotInheritable Class CreateUserRequest
        ''' <summary>Gets or sets the user name to create.</summary>
        Public Property Name As String = ""
    End Class

    ''' <summary>Carries the VB.NET sample create-user response.</summary>
    Public NotInheritable Class CreateUserResponse
        ''' <summary>Initializes a new instance of the <see cref="CreateUserResponse" /> class.</summary>
        Public Sub New()
        End Sub

        ''' <summary>Initializes a new instance of the <see cref="CreateUserResponse" /> class.</summary>
        ''' <param name="id">The generated sample user identifier.</param>
        ''' <param name="name">The created user name.</param>
        Public Sub New(id As String, name As String)
            Me.Id = id
            Me.Name = name
        End Sub

        ''' <summary>Gets or sets the generated sample user identifier.</summary>
        Public Property Id As String = ""

        ''' <summary>Gets or sets the created user name.</summary>
        Public Property Name As String = ""
    End Class

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
