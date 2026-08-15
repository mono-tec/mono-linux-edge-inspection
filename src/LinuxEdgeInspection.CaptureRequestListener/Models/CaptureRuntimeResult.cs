namespace LinuxEdgeInspection.CaptureRequestListener.Models;

/// <summary>
/// 撮影Runtimeが出力した実行結果を表します。
/// </summary>
/// <param name="Succeeded">
/// 撮影処理に成功したかどうかです。
/// </param>
/// <param name="FilePath">
/// 撮影した画像ファイルの保存先パスです。
/// 撮影に失敗した場合は<c>null</c>です。
/// </param>
/// <param name="CompletedAt">
/// 撮影Runtimeの処理が完了した日時です。
/// </param>
/// <param name="ErrorCode">
/// エラーコードです。成功時は<c>null</c>です。
/// </param>
/// <param name="ErrorMessage">
/// エラーメッセージです。成功時は<c>null</c>です。
/// </param>
public sealed record CaptureRuntimeResult(
    bool Succeeded,
    string? FilePath,
    DateTimeOffset CompletedAt,
    string? ErrorCode,
    string? ErrorMessage);