using Kawa.Abstractions;

namespace Kawa.Cli;

/// <summary>Uses exit code one for every application failure.</summary>
public sealed class KawaDefaultCliExitCodeMapper : IKawaCliExitCodeMapper
{
    /// <inheritdoc />
    public int MapError(KawaError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return 1;
    }
}
