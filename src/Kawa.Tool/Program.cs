using System.CommandLine;
using Kawa.Abstractions;
using Kawa.Cli;

var schema = new Option<string>("--schema") { Required = true, Description = "Generated .proto file under obj/." };
var output = new Option<string>("--output") { DefaultValueFactory = _ => "Protos/kawa.generated.proto" };
var overwrite = new Option<bool>("--force");
var export = new Command("export", "Explicitly export an already generated protobuf schema.");
export.Options.Add(schema);
export.Options.Add(output);
export.Options.Add(overwrite);
export.SetKawaAction(new ExportSchema(), parse => new ExportSchema.Request(
    parse.GetValue(schema)!, parse.GetValue(output)!, parse.GetValue(overwrite)));
var grpc = new Command("grpc");
grpc.Subcommands.Add(export);
var root = new RootCommand("Kawa development tools.");
root.Subcommands.Add(grpc);
return await root.InvokeKawaAsync(args);

public sealed class ExportSchema : IUseCase<ExportSchema.Request, ExportSchema.Response>
{
    public sealed record Request(string Schema, string Output, bool Force);
    public sealed record Response(string Output);
    public Task<KawaResult<Response>> ExecuteAsync(Request request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(request.Schema))
            return Task.FromResult(KawaResult<Response>.Failure(new KawaError(KawaErrorKind.NotFound, "Generated schema not found. Build the project first.")));
        if (File.Exists(request.Output) && !request.Force)
            return Task.FromResult(KawaResult<Response>.Failure(new KawaError(KawaErrorKind.Conflict, "Output exists. Use --force to replace it.")));
        var path = Path.GetFullPath(request.Output);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Copy(request.Schema, path, overwrite: request.Force);
        return Task.FromResult(KawaResult<Response>.Success(new Response(path)));
    }
}
