using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// systemdを使用して撮影Runtimeを起動します。
/// </summary>
public sealed class SystemdCaptureRuntimeLauncher
    : ICaptureRuntimeLauncher
{
    private const string LaunchFailedErrorCode =
        "CAPTURE_RUNTIME_LAUNCH_FAILED";

    private const string TimeoutErrorCode =
        "CAPTURE_RUNTIME_TIMEOUT";

    private const string CancelledErrorCode =
        "CAPTURE_RUNTIME_CANCELLED";

    private readonly ISystemCommandRunner _commandRunner;
    private readonly string _systemctlPath;
    private readonly string _serviceName;
    private readonly TimeSpan _timeout;

    /// <summary>
    /// <see cref="SystemdCaptureRuntimeLauncher"/>を初期化します。
    /// </summary>
    /// <param name="commandRunner">
    /// 外部システムコマンドを実行するサービスです。
    /// </param>
    /// <param name="systemctlPath">
    /// systemctlコマンドのパスです。
    /// </param>
    /// <param name="serviceName">
    /// 起動対象のsystemd Unit名です。
    /// </param>
    /// <param name="timeout">
    /// systemctl実行のタイムアウト時間です。
    /// </param>
    public SystemdCaptureRuntimeLauncher(
        ISystemCommandRunner commandRunner,
        string systemctlPath,
        string serviceName,
        TimeSpan timeout)
    {
        _commandRunner = commandRunner
            ?? throw new ArgumentNullException(
                nameof(commandRunner));

        ArgumentException.ThrowIfNullOrWhiteSpace(
            systemctlPath);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            serviceName);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                "タイムアウト時間は0より大きい値を指定してください。");
        }

        _systemctlPath = systemctlPath;
        _serviceName = serviceName;
        _timeout = timeout;
    }

    /// <inheritdoc />
    public async Task<CaptureRuntimeLaunchResult> LaunchAsync(
        CancellationToken cancellationToken = default)
    {
        var startedAt =
            DateTimeOffset.Now;

        var executionResult =
            await _commandRunner.ExecuteAsync(
                _systemctlPath,
                [
                    "restart",
                    _serviceName
                ],
                _timeout,
                cancellationToken);

        var completedAt =
            DateTimeOffset.Now;

        if (executionResult.TimedOut)
        {
            return new CaptureRuntimeLaunchResult(
                Succeeded: false,
                ExitCode: executionResult.ExitCode,
                StartedAt: startedAt,
                CompletedAt: completedAt,
                ErrorCode: TimeoutErrorCode,
                ErrorMessage:
                    "撮影Runtimeの起動処理がタイムアウトしました。");
        }

        if (executionResult.Cancelled)
        {
            return new CaptureRuntimeLaunchResult(
                Succeeded: false,
                ExitCode: executionResult.ExitCode,
                StartedAt: startedAt,
                CompletedAt: completedAt,
                ErrorCode: CancelledErrorCode,
                ErrorMessage:
                    "撮影Runtimeの起動処理がキャンセルされました。");
        }

        if (!executionResult.Succeeded)
        {
            var errorMessage =
                string.IsNullOrWhiteSpace(
                    executionResult.StandardError)
                    ? "撮影Runtimeの起動に失敗しました。"
                    : executionResult.StandardError.Trim();

            return new CaptureRuntimeLaunchResult(
                Succeeded: false,
                ExitCode: executionResult.ExitCode,
                StartedAt: startedAt,
                CompletedAt: completedAt,
                ErrorCode: LaunchFailedErrorCode,
                ErrorMessage: errorMessage);
        }

        return new CaptureRuntimeLaunchResult(
            Succeeded: true,
            ExitCode: executionResult.ExitCode,
            StartedAt: startedAt,
            CompletedAt: completedAt,
            ErrorCode: null,
            ErrorMessage: null);
    }
}
