using System.Text.Json;
using System.Text.Json.Serialization;
using Kawa.Abstractions;

namespace Kawa.Cli;

/// <summary>Writes successful results to stdout and failures to stderr.</summary>
public sealed class KawaCliResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions ErrorJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
    private readonly TextWriter output;
    private readonly TextWriter error;
    private readonly IKawaCliExitCodeMapper exitCodeMapper;

    /// <summary>Creates a writer without taking ownership of its streams.</summary>
    public KawaCliResultWriter(TextWriter output, TextWriter error, IKawaCliExitCodeMapper? exitCodeMapper = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        this.output = output;
        this.error = error;
        this.exitCodeMapper = exitCodeMapper ?? new KawaDefaultCliExitCodeMapper();
    }

    /// <summary>Writes one result and returns its exit code. JSON is the default success format.</summary>
    public async Task<int> WriteAsync<TResponse>(
        KawaResult<TResponse> result,
        IKawaCliTextFormatter<TResponse>? formatter = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure)
        {
            var failure = result.Error!;
            var publicError = failure.Kind == KawaErrorKind.Unknown || !Enum.IsDefined(failure.Kind)
                ? new KawaError(KawaErrorKind.Unknown, "An unexpected error occurred.")
                : failure;
            // Compute the complete payload before touching the stream.
            var json = JsonSerializer.Serialize(publicError, ErrorJsonOptions);
            var code = exitCodeMapper.MapError(failure);
            if (code == 0)
            {
                throw new InvalidOperationException("A failure exit code must be nonzero.");
            }

            await error.WriteLineAsync(json);
            return code;
        }

        var text = formatter is null
            ? JsonSerializer.Serialize(result.Value, JsonOptions)
            : formatter.Format(result.Value!);
        await output.WriteLineAsync(text);
        return 0;
    }
}
