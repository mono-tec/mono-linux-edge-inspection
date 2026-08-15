namespace LinuxEdgeInspection.Contracts.Capture;

/// <summary>
/// 1回の撮影要求に対する処理結果を表します。
/// </summary>
public sealed record CaptureResult(
    string RequestId,
    int CaptureIndex,
    bool Succeeded,
    DateTimeOffset CompletedAt,
    string? ErrorCode,
    string? ErrorMessage);
