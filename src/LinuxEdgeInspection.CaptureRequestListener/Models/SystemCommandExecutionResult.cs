namespace LinuxEdgeInspection.CaptureRequestListener.Models;

/// <summary>
/// 外部システムコマンドの実行結果を表します。
/// </summary>
/// <param name="ExitCode">
/// コマンドの終了コードです。
/// 終了コードを取得できなかった場合は<c>null</c>です。
/// </param>
/// <param name="StandardOutput">
/// 標準出力です。
/// </param>
/// <param name="StandardError">
/// 標準エラー出力です。
/// </param>
/// <param name="Duration">
/// コマンドの実行時間です。
/// </param>
/// <param name="TimedOut">
/// タイムアウトしたかどうかです。
/// </param>
/// <param name="Cancelled">
/// キャンセルされたかどうかです。
/// </param>
public sealed record SystemCommandExecutionResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    bool TimedOut,
    bool Cancelled)
{
    /// <summary>
    /// コマンドが正常終了したかどうかを取得します。
    /// </summary>
    public bool Succeeded =>
        ExitCode == 0 &&
        !TimedOut &&
        !Cancelled;
}