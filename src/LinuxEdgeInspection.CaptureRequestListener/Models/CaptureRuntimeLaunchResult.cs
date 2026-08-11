namespace LinuxEdgeInspection.CaptureRequestListener.Models;

/// <summary>
/// 撮影Runtimeの起動結果を表します。
/// </summary>
/// <param name="Succeeded">
/// 撮影Runtimeの起動に成功したかどうかです。
/// </param>
/// <param name="ExitCode">
/// 撮影Runtimeまたは起動コマンドの終了コードです。
/// 終了コードを取得できない場合は<c>null</c>です。
/// </param>
/// <param name="StartedAt">
/// 起動処理を開始した日時です。
/// </param>
/// <param name="CompletedAt">
/// 起動処理が完了した日時です。
/// </param>
/// <param name="ErrorCode">
/// エラーコードです。成功時は<c>null</c>です。
/// </param>
/// <param name="ErrorMessage">
/// エラーメッセージです。成功時は<c>null</c>です。
/// </param>
public sealed record CaptureRuntimeLaunchResult(
    bool Succeeded,
    int? ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string? ErrorCode,
    string? ErrorMessage);