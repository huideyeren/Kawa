using Kawa.Abstractions;

namespace Kawa.Cli;

/// <summary>Maps application failures to process exit codes. Success always returns zero.</summary>
public interface IKawaCliExitCodeMapper
{
    /// <summary>Returns the nonzero process exit code for a failure.</summary>
    int MapError(KawaError error);
}
