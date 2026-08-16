using System.Diagnostics;
using LinuxEdgeInspection.Plugin.LogViewer.Options;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public sealed class JournalctlProcessRunner : IJournalctlProcessRunner
{
    private readonly TimeSpan _timeout;

    public JournalctlProcessRunner(IOptions<JournalctlOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Value.TimeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "TimeoutSeconds must be greater than zero.");
        }

        _timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
    }

    public async Task<JournalctlProcessResult> RunAsync(
        JournalctlCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var startInfo = new ProcessStartInfo
        {
            FileName = command.ExecutablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("journalctlを起動できませんでした。");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutSource = new CancellationTokenSource(_timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            if (!cancellationToken.IsCancellationRequested &&
                timeoutSource.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"journalctl timed out after {_timeout}.");
            }

            throw;
        }

        return new JournalctlProcessResult(
            process.ExitCode,
            await outputTask,
            await errorTask);
    }
}
