using Kawa.Abstractions;

/// <summary>
/// Creates a simple sample user response.
/// </summary>
public sealed class CreateUserUseCase : IUseCase<CreateUserRequest, CreateUserResponse>
{
    /// <inheritdoc />
    public Task<KawaResult<CreateUserResponse>> ExecuteAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new CreateUserResponse($"Created user {request.Name}.");
        return Task.FromResult(KawaResult<CreateUserResponse>.Success(response));
    }
}
