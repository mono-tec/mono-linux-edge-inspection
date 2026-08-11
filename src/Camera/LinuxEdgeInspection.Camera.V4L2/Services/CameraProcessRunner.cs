using LinuxEdgeInspection.Camera.V4L2.Models;
using System.Diagnostics;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// 外部プロセスを起動し、終了コード、標準出力、標準エラーを取得します。
/// </summary>
public sealed class CameraProcessRunner : ICameraProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessExecutionResult> ExecuteAsync(
        ProcessExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException(
                "実行するコマンドを指定してください。",
                nameof(request));
        }

        if (request.Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "タイムアウト時間は0より大きい値を指定してください。");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        using var timeoutCancellationTokenSource =
            new CancellationTokenSource(request.Timeout);

        using var linkedCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCancellationTokenSource.Token);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!process.Start())
            {
                stopwatch.Stop();

                return new ProcessExecutionResult(
                    ExitCode: null,
                    StandardOutput: string.Empty,
                    StandardError:
                        $"プロセスを開始できませんでした。Command: {request.FileName}",
                    Duration: stopwatch.Elapsed,
                    TimedOut: false,
                    Cancelled: false);
            }

            var standardOutputTask =
                process.StandardOutput.ReadToEndAsync();

            var standardErrorTask =
                process.StandardError.ReadToEndAsync();

            try
            {
                await process.WaitForExitAsync(
                    linkedCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                var cancelled = cancellationToken.IsCancellationRequested;
                var timedOut =
                    !cancelled &&
                    timeoutCancellationTokenSource.IsCancellationRequested;

                TryKillProcess(process);

                await WaitForExitSafelyAsync(process);

                var cancelledStandardOutput =
                    await standardOutputTask;

                var cancelledStandardError =
                    await standardErrorTask;

                stopwatch.Stop();

                return new ProcessExecutionResult(
                    ExitCode: null,
                    StandardOutput: cancelledStandardOutput,
                    StandardError: cancelledStandardError,
                    Duration: stopwatch.Elapsed,
                    TimedOut: timedOut,
                    Cancelled: cancelled);
            }

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;

            stopwatch.Stop();

            return new ProcessExecutionResult(
                ExitCode: process.ExitCode,
                StandardOutput: standardOutput,
                StandardError: standardError,
                Duration: stopwatch.Elapsed,
                TimedOut: false,
                Cancelled: false);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();

            return new ProcessExecutionResult(
                ExitCode: null,
                StandardOutput: string.Empty,
                StandardError: exception.Message,
                Duration: stopwatch.Elapsed,
                TimedOut: false,
                Cancelled: false);
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // プロセスがすでに終了している場合は何もしません。
        }
        catch (NotSupportedException)
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
    }

    private static async Task WaitForExitSafelyAsync(
        Process process)
    {
        try
        {
            await process.WaitForExitAsync(
                CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // プロセスが開始されていない、またはすでに破棄された場合は
            // 追加処理を行いません。
        }
    }
}