namespace LinuxEdgeInspection.CaptureRequestListener.Models;

/// <summary>
/// 撮影要求に対する処理結果を表します。
/// </summary>
/// <param name="RequestId">
/// 対応する撮影要求番号です。
/// </param>
/// <param name="Succeeded">
/// 撮影処理が成功したかどうかです。
/// </param>
/// <param name="CompletedAt">
/// 撮影処理が完了した日時です。
/// </param>
/// <param name="ErrorCode">
/// エラーコードです。成功時は<c>null</c>です。
/// </param>
/// <param name="ErrorMessage">
/// エラーメッセージです。成功時は<c>null</c>です。
/// </param>
public sealed record CaptureResult(
    long RequestId,
    bool Succeeded,
    DateTimeOffset CompletedAt,
    string? ErrorCode,
    string? ErrorMessage);