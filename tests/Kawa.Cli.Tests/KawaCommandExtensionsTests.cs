using System.CommandLine;
using Kawa.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kawa.Cli.Tests;

public sealed class KawaCommandExtensionsTests
{
    [Fact]
    public async Task Success_WritesCamelCaseJsonOnlyToStdout()
    {
        var option = new Option<string>("--value") { Required = true };
        var command = new RootCommand();
        command.Options.Add(option);
        command.SetKawaAction(new Echo(), parse => parse.GetValue(option)!);
        var (code, output, error) = await Invoke(command, ["--value", "ok"]);
        Assert.Equal(0, code);
        Assert.Equal("{\"value\":\"ok\"}" + Environment.NewLine, output);
        Assert.Empty(error);
    }

    [Fact]
    public async Task Validation_WritesOnlyStderrAndReturnsOne()
    {
        var command = new RootCommand();
        command.SetKawaAction(new Echo(), _ => "");
        var (code, output, error) = await Invoke(command, []);
        Assert.Equal(1, code);
        Assert.Empty(output);
        Assert.Contains("Value is required.", error);
    }

    [Fact]
    public async Task ParseError_DoesNotExecuteUseCaseOrWriteStdout()
    {
        var called = false;
        var command = new RootCommand();
        command.SetKawaAction(new Echo(), _ => { called = true; return "ok"; });
        var (code, output, error) = await Invoke(command, ["--unknown"]);
        Assert.Equal(1, code);
        Assert.False(called);
        Assert.Empty(output);
        Assert.NotEmpty(error);
    }

    [Fact]
    public async Task UnexpectedException_DoesNotExposeDetails()
    {
        var command = new RootCommand();
        command.SetKawaAction(new Echo(), _ => throw new InvalidOperationException("secret-password"));
        var (code, output, error) = await Invoke(command, []);
        Assert.Equal(1, code);
        Assert.Empty(output);
        Assert.Contains("An unexpected error occurred.", error);
        Assert.DoesNotContain("secret-password", error);
    }

    [Fact]
    public async Task UnexpectedException_UsesCustomExitCodePolicy()
    {
        var command = new RootCommand();
        command.SetKawaAction(new Echo(), _ => throw new InvalidOperationException("secret"), new CustomExitCodeMapper());
        var result = await Invoke(command, []);
        Assert.Equal(23, result.Code);
        Assert.Empty(result.Output);
        Assert.DoesNotContain("secret", result.Error);
    }

    [Fact]
    public async Task FormatterException_DoesNotWritePartialStdout()
    {
        var command = new RootCommand();
        command.SetKawaAction(new Echo(), _ => "ok", formatter: new FailingFormatter());
        var result = await Invoke(command, []);
        Assert.Equal(1, result.Code);
        Assert.Empty(result.Output);
        Assert.Contains("An unexpected error occurred.", result.Error);
        Assert.DoesNotContain("secret", result.Error);
    }

    [Fact]
    public async Task Cancellation_ReachesUseCaseAndWritesOnlyStderr()
    {
        using var cancellation = new CancellationTokenSource();
        var useCase = new Cancellable(cancellation);
        var command = new RootCommand();
        command.SetKawaAction(useCase, _ => "ok");
        var (code, output, error) = await Invoke(command, [], cancellation.Token);
        Assert.True(useCase.SawCancellation);
        Assert.Equal(1, code);
        Assert.Empty(output);
        Assert.Contains("Cancelled", error);
    }

    [Fact]
    public async Task Services_ResolveCustomExitCodeAndTextFormatter()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUseCase<string, Response>, Echo>();
        services.AddSingleton<IKawaCliExitCodeMapper, CustomExitCodeMapper>();
        services.AddSingleton<IKawaCliTextFormatter<Response>, TextFormatter>();
        using var provider = services.BuildServiceProvider();
        var command = new RootCommand();
        command.SetKawaAction<string, Response>(provider, _ => "ok");
        var success = await Invoke(command, []);
        Assert.Equal(0, success.Code);
        Assert.Equal("Value: ok" + Environment.NewLine, success.Output);
        Assert.Empty(success.Error);
        command.SetKawaAction<string, Response>(provider, _ => "");
        var failure = await Invoke(command, []);
        Assert.Equal(23, failure.Code);
        Assert.Empty(failure.Output);
        Assert.NotEmpty(failure.Error);
    }

    [Fact]
    public async Task Services_DisposeRequestScopeAfterInvocation()
    {
        DisposableUseCase.Disposals = 0;
        var services = new ServiceCollection();
        services.AddScoped<IUseCase<string, Response>, DisposableUseCase>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var command = new RootCommand();
        command.SetKawaAction<string, Response>(provider, _ => "ok");
        Assert.Equal(0, (await Invoke(command, [])).Code);
        Assert.Equal(1, DisposableUseCase.Disposals);
    }

    private static async Task<(int Code, string Output, string Error)> Invoke(
        Command command, string[] arguments, CancellationToken cancellationToken = default)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var configuration = new InvocationConfiguration { Output = output, Error = error };
        var code = await command.InvokeKawaAsync(arguments, configuration, cancellationToken);
        return (code, output.ToString(), error.ToString());
    }

    public sealed record Response(string Value);

    private sealed class Echo : IUseCase<string, Response>
    {
        public Task<KawaResult<Response>> ExecuteAsync(string request, CancellationToken cancellationToken = default) =>
            Task.FromResult(request.Length == 0
                ? KawaResult<Response>.Failure(new KawaError(KawaErrorKind.Validation, "Value is required."))
                : KawaResult<Response>.Success(new Response(request)));
    }

    private sealed class Cancellable(CancellationTokenSource cancellation) : IUseCase<string, Response>
    {
        public bool SawCancellation { get; private set; }
        public Task<KawaResult<Response>> ExecuteAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellation.Cancel();
            SawCancellation = cancellationToken.IsCancellationRequested;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(KawaResult<Response>.Success(new Response(request)));
        }
    }

    private sealed class CustomExitCodeMapper : IKawaCliExitCodeMapper
    {
        public int MapError(KawaError error) => 23;
    }

    private sealed class TextFormatter : IKawaCliTextFormatter<Response>
    {
        public string Format(Response response) => $"Value: {response.Value}";
    }

    private sealed class FailingFormatter : IKawaCliTextFormatter<Response>
    {
        public string Format(Response response) => throw new InvalidOperationException("secret");
    }

    private sealed class DisposableUseCase : IUseCase<string, Response>, IDisposable
    {
        public static int Disposals;
        public void Dispose() => Disposals++;
        public Task<KawaResult<Response>> ExecuteAsync(string request, CancellationToken cancellationToken = default) =>
            Task.FromResult(KawaResult<Response>.Success(new Response(request)));
    }
}
