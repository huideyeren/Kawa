using Kawa.Abstractions;
using Xunit;

namespace Kawa.Cli.Tests;

public sealed class KawaCliResultWriterTests
{
    [Fact]
    public async Task Success_UsesWebJsonDefaultsForEnumValues()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var writer = new KawaCliResultWriter(output, error);
        await writer.WriteAsync(KawaResult<DayOfWeek>.Success(DayOfWeek.Monday));
        Assert.Equal("1" + Environment.NewLine, output.ToString());
        Assert.Empty(error.ToString());
    }

    [Theory]
    [InlineData(KawaErrorKind.Unknown)]
    [InlineData((KawaErrorKind)999)]
    public async Task InternalErrors_RedactSensitiveMessages(KawaErrorKind kind)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var writer = new KawaCliResultWriter(output, error);
        Assert.Equal(1, await writer.WriteAsync(KawaResult<string>.Failure(new KawaError(kind, "secret"))));
        Assert.Empty(output.ToString());
        Assert.DoesNotContain("secret", error.ToString());
        Assert.Contains("An unexpected error occurred.", error.ToString());
    }

    [Fact]
    public async Task Success_WithNullValueWritesJsonNull()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var writer = new KawaCliResultWriter(output, error);
        Assert.Equal(0, await writer.WriteAsync(KawaResult<string?>.Success(null)));
        Assert.Equal("null" + Environment.NewLine, output.ToString());
        Assert.Empty(error.ToString());
    }
}
