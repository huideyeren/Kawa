using Kawa.Abstractions;

/// <summary>
/// Creates a simple sample user response.
/// </summary>
[KawaUseCase(
    "users.create",
    Summary = "Create user",
    Description = "Creates a sample user response.",
    Version = "v1",
    Tags = new[] { "Users" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "The supplied user fields are invalid.")]
public sealed class CreateUser : IUseCase<CreateUser.Request, CreateUser.Response>
{
    /// <summary>
    /// Carries the name for the sample create-user request.
    /// </summary>
    /// <param name="Name">The user name to create.</param>
    public sealed record Request(string Name);

    /// <summary>
    /// Carries the sample create-user response message.
    /// </summary>
    /// <param name="Message">The response message.</param>
    public sealed record Response(string Message);

    /// <inheritdoc />
    public Task<KawaResult<Response>> ExecuteAsync(
        Request request,
        CancellationToken cancellationToken = default)
    {
        var response = new Response($"Created user {request.Name}.");
        return Task.FromResult(KawaResult<Response>.Success(response));
    }
}
