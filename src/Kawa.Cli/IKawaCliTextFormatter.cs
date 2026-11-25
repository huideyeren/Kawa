namespace Kawa.Cli;

/// <summary>Opts a response contract into human-readable CLI output.</summary>
/// <typeparam name="TResponse">The response contract to format.</typeparam>
public interface IKawaCliTextFormatter<in TResponse>
{
    /// <summary>Formats a successful response, without a trailing newline.</summary>
    string Format(TResponse response);
}
