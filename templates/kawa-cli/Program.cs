using System.CommandLine;
using Kawa.Abstractions;
using Kawa.Cli;

var message = new Option<string>("--message") { DefaultValueFactory = _ => "hello" };
var command = new RootCommand("Send a chat message through a Kawa use case.");
command.Options.Add(message);
command.SetKawaAction(new SendMessage(), parse => parse.GetValue(message)!);
return await command.InvokeKawaAsync(args);

public sealed class SendMessage : IUseCase<string, SendMessage.Response>
{
    public sealed record Response(string Message);
    public Task<KawaResult<Response>> ExecuteAsync(string request, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.IsNullOrWhiteSpace(request)
            ? KawaResult<Response>.Failure(new KawaError(KawaErrorKind.Validation, "Message is required."))
            : KawaResult<Response>.Success(new Response(request)));
}
