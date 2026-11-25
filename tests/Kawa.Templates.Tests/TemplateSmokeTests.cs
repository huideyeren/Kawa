using System.Diagnostics;
using Xunit;

namespace Kawa.Templates.Tests;

public sealed class TemplateSmokeTests
{
    [Fact]
    public async Task PackagedTemplates_GenerateRestoreBuildAndRun()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Kawa.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var start = new ProcessStartInfo("python3")
        {
            WorkingDirectory = root.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("eng/test-templates.py");
        start.ArgumentList.Add("--configuration");
        start.ArgumentList.Add(new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }
        Assert.True(process.ExitCode == 0, await output + "\n" + await error);
    }
}
