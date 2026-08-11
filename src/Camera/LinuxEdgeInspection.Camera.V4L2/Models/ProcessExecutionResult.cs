namespace LinuxEdgeInspection.Camera.V4L2.Models;

/// <summary>
/// 外部プロセスの実行結果を表します。
/// </summary>
/// <param name="ExitCode">
/// プロセスの終了コードです。
/// プロセスを開始できなかった場合やタイムアウト時は<c>null</c>です。
/// </param>
/// <param name="StandardOutput">
/// 標準出力へ出力された内容です。
/// </param>
/// <param name="StandardError">
/// 標準エラー出力へ出力された内容です。
/// </param>
/// <param name="Duration">
/// プロセスの実行に要した時間です。
/// </param>
/// <param name="TimedOut">
/// タイムアウトによりプロセスを終了した場合は<c>true</c>です。
/// </param>
/// <param name="Cancelled">
/// 呼び出し元からのキャンセルにより終了した場合は<c>true</c>です。
/// </param>
public sealed record ProcessExecutionResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    bool TimedOut,
    bool Cancelled)
{
    /// <summary>
    /// プロセスが正常終了したかを取得します。
    /// </summary>
    public bool Succeeded =>
        ExitCode == 0 &&
        !TimedOut &&
        !Cancelled;
}