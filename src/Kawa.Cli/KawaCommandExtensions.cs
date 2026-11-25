using System.CommandLine;
using Kawa.Abstractions;
using Kawa.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Kawa.Cli;

/// <summary>Connects System.CommandLine commands to transport-independent Kawa use cases.</summary>
public static class KawaCommandExtensions
{
    /// <summary>Binds an explicitly supplied use case to a command.</summary>
    public static Command SetKawaAction<TRequest, TResponse>(
        this Command command,
        IUseCase<TRequest, TResponse> useCase,
        Func<ParseResult, TRequest> bindRequest,
        IKawaCliExitCodeMapper? exitCodeMapper = null,
        IKawaCliTextFormatter<TResponse>? formatter = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(bindRequest);
        command.SetAction((parse, token) => ExecuteAsync(
            parse, token, new UseCaseExecutor(), useCase, bindRequest, exitCodeMapper, formatter));
        return command;
    }

    /// <summary>Resolves a use case and optional output policies in a new scope per invocation.</summary>
    public static Command SetKawaAction<TRequest, TResponse>(
        this Command command,
        IServiceProvider services,
        Func<ParseResult, TRequest> bindRequest)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(bindRequest);
        command.SetAction(async (parse, token) =>
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var scoped = scope.ServiceProvider;
                return await ExecuteAsync(parse, token,
                    scoped.GetService<UseCaseExecutor>() ?? new UseCaseExecutor(),
                    scoped.GetRequiredService<IUseCase<TRequest, TResponse>>(), bindRequest,
                    scoped.GetService<IKawaCliExitCodeMapper>(),
                    scoped.GetService<IKawaCliTextFormatter<TResponse>>());
            }
            catch (Exception)
            {
                // Includes resolution/disposal failures; never print exception details.
                return await WriteUnexpectedAsync(parse.InvocationConfiguration.Error);
            }
        });
        return command;
    }

    /// <summary>Invokes a command, keeping parse failures on stderr without help output on stdout.</summary>
    public static async Task<int> InvokeKawaAsync(
        this Command command,
        string[] arguments,
        InvocationConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(arguments);
        configuration ??= new InvocationConfiguration();
        var parse = command.Parse(arguments);
        if (parse.Errors.Count > 0)
        {
            foreach (var error in parse.Errors)
            {
                await configuration.Error.WriteLineAsync(error.Message);
            }
            return 1;
        }

        try
        {
            return await parse.InvokeAsync(configuration, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await new KawaCliResultWriter(configuration.Output, configuration.Error)
                .WriteAsync(KawaResult<object>.Failure(new KawaError(KawaErrorKind.Cancelled, "Operation cancelled.")));
        }
    }

    private static async Task<int> ExecuteAsync<TRequest, TResponse>(
        ParseResult parse, CancellationToken token, UseCaseExecutor executor,
        IUseCase<TRequest, TResponse> useCase, Func<ParseResult, TRequest> bindRequest,
        IKawaCliExitCodeMapper? exitCodeMapper, IKawaCliTextFormatter<TResponse>? formatter)
    {
        var writer = new KawaCliResultWriter(
            parse.InvocationConfiguration.Output, parse.InvocationConfiguration.Error, exitCodeMapper);
        try
        {
            var result = await executor.ExecuteAsync(useCase, bindRequest(parse), token);
            return await writer.WriteAsync(result, formatter);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return await writer.WriteAsync(KawaResult<TResponse>.Failure(
                new KawaError(KawaErrorKind.Cancelled, "Operation cancelled.")));
        }
        catch (Exception)
        {
            try
            {
                return await writer.WriteAsync(KawaResult<TResponse>.Failure(
                    new KawaError(KawaErrorKind.Unknown, "An unexpected error occurred.")));
            }
            catch (Exception)
            {
                // A broken policy must not turn failure into success or disclose its exception.
                return await WriteUnexpectedAsync(parse.InvocationConfiguration.Error);
            }
        }
    }

    private static Task<int> WriteUnexpectedAsync(TextWriter error) =>
        new KawaCliResultWriter(TextWriter.Null, error).WriteAsync(
            KawaResult<object>.Failure(new KawaError(KawaErrorKind.Unknown, "An unexpected error occurred.")));
}
