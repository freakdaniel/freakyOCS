using System.Diagnostics;
using System.Text;

namespace OcsNet.Core.Services;

public sealed record ProcessResult(string Output, string Error, int ExitCode)
{
    public bool Success => ExitCode == 0;
}

public sealed class ProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string executable,
        IEnumerable<string>? args = null,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            WorkingDirectory       = workingDirectory ?? Directory.GetCurrentDirectory(),
        };

        foreach (var arg in args ?? [])
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived  += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(stdout.ToString(), stderr.ToString(), process.ExitCode);
    }

    public async Task<ProcessResult> RunStreamingAsync(
        string executable,
        IEnumerable<string>? args = null,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            WorkingDirectory       = workingDirectory ?? Directory.GetCurrentDirectory(),
        };

        foreach (var arg in args ?? [])
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stdout.AppendLine(e.Data);
            onOutput?.Invoke(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stderr.AppendLine(e.Data);
            onError?.Invoke(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(stdout.ToString(), stderr.ToString(), process.ExitCode);
    }
}
