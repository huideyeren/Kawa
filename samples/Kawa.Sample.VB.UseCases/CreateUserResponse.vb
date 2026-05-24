Namespace Kawa.Sample.VB.UseCases
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
End Namespace
