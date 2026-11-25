namespace Kawa.Core.Tests;

/// <summary>Confirms compatibility CI executes tests on the requested runtime.</summary>
public sealed class RuntimeCompatibilityTests
{
    /// <summary>Prevents the .NET 11 job from silently running its net10.0 tests on .NET 10.</summary>
    [Fact]
    public void Runtime_MatchesCompatibilityJobRequirement()
    {
        var expected = Environment.GetEnvironmentVariable("KAWA_EXPECTED_RUNTIME_MAJOR");
        if (string.IsNullOrEmpty(expected))
        {
            Assert.True(Environment.Version.Major >= 10);
            return;
        }

        Assert.Equal(int.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), Environment.Version.Major);
    }
}
