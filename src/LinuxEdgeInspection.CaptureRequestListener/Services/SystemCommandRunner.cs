using LinuxEdgeInspection.CaptureRequestListener.Models;
using System.Diagnostics;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// 外部システムコマンドをプロセスとして実行します。
/// </summary>
public sealed class SystemCommandRunner
    : ISystemCommandRunner
{
    /// <inheritdoc />
    public async Task<SystemCommandExecutionResult> ExecuteAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            fileName);

        ArgumentNullException.ThrowIfNull(
            arguments);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                "タイムアウト時間は0より大きい値を指定してください。");
        }

        var stopwatch =
            Stopwatch.StartNew();

        using var process =
            new Process
            {
                StartInfo =
                    new ProcessStartInfo
                    {
                        FileName = fileName,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    },
                EnableRaisingEvents = true
            };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(
                argument);
        }

        try
        {
            if (!process.Start())
            {
                stopwatch.Stop();

                return new SystemCommandExecutionResult(
                    ExitCode: null,
                    StandardOutput: string.Empty,
                    StandardError:
                        "外部コマンドを開始できませんでした。",
                    Duration: stopwatch.Elapsed,
                    TimedOut: false,
                    Cancelled: false);
            }

            var standardOutputTask =
                process.StandardOutput.ReadToEndAsync();

            var standardErrorTask =
                process.StandardError.ReadToEndAsync();

            using var timeoutCancellationTokenSource =
                new CancellationTokenSource(
                    timeout);

            using var linkedCancellationTokenSource =
                CancellationTokenSource
                    .CreateLinkedTokenSource(
                        cancellationToken,
                        timeoutCancellationTokenSource.Token);

            try
            {
                await process.WaitForExitAsync(
                    linkedCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
                when (timeoutCancellationTokenSource
                    .IsCancellationRequested &&
                    !cancellationToken
                        .IsCancellationRequested)
            {
                TryKillProcess(process);

                var standardOutput =
                    await standardOutputTask;

                var standardError =
                    await standardErrorTask;

                stopwatch.Stop();

                return new SystemCommandExecutionResult(
                    ExitCode: null,
                    StandardOutput: standardOutput,
                    StandardError: standardError,
                    Duration: stopwatch.Elapsed,
                    TimedOut: true,
                    Cancelled: false);
            }
            catch (OperationCanceledException)
                when (cancellationToken
                    .IsCancellationRequested)
            {
                TryKillProcess(process);

                var standardOutput =
                    await standardOutputTask;

                var standardError =
                    await standardErrorTask;

                stopwatch.Stop();

                return new SystemCommandExecutionResult(
                    ExitCode: null,
                    StandardOutput: standardOutput,
                    StandardError: standardError,
                    Duration: stopwatch.Elapsed,
                    TimedOut: false,
                    Cancelled: true);
            }

            var completedStandardOutput =
                await standardOutputTask;

            var completedStandardError =
                await standardErrorTask;

            stopwatch.Stop();

            return new SystemCommandExecutionResult(
                ExitCode: process.ExitCode,
                StandardOutput:
                    completedStandardOutput,
                StandardError:
                    completedStandardError,
                Duration: stopwatch.Elapsed,
                TimedOut: false,
                Cancelled: false);
        }
        catch (Exception exception)
            when (exception is
                System.ComponentModel.Win32Exception or
                InvalidOperationException)
        {
            stopwatch.Stop();

            return new SystemCommandExecutionResult(
                ExitCode: null,
                StandardOutput: string.Empty,
                StandardError: exception.Message,
                Duration: stopwatch.Elapsed,
                TimedOut: false,
                Cancelled: false);
        }
    }

    private static void TryKillProcess(
        Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(
                    entireProcessTree: true);
            }
        }
        catch
        {
            // 終了済み、または終了処理中の例外は無視します。
        }
    }
}