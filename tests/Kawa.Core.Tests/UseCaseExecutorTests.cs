using System.Threading;
using System.Threading.Tasks;
using Kawa.Abstractions;
using Kawa.Core;

namespace Kawa.Core.Tests;

/// <summary>
/// Verifies use case execution through the core executor.
/// </summary>
public class UseCaseExecutorTests
{
    /// <summary>
    /// Verifies that the executor returns a successful use case result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ReturnsUseCaseResult()
    {
        var useCase = new ExampleUseCase();
        var executor = new UseCaseExecutor();

        var result = await executor.ExecuteAsync(useCase, 42, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Value: 42", result.Value);
    }

    /// <summary>
    /// Verifies that the executor returns a failed use case result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PropagatesFailureResult()
    {
        var useCase = new FailingUseCase();
        var executor = new UseCaseExecutor();

        var result = await executor.ExecuteAsync(useCase, 42, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Equal("Invalid input", result.Error.Message);
    }

    private sealed class ExampleUseCase : IUseCase<int, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(int request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KawaResult<string>.Success($"Value: {request}"));
        }
    }

    private sealed class FailingUseCase : IUseCase<int, string>
    {
        public Task<KawaResult<string>> ExecuteAsync(int request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KawaResult<string>.Failure(new KawaError(KawaErrorKind.Validation, "Invalid input")));
        }
    }
}
