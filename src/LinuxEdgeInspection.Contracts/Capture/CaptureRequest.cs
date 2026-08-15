namespace LinuxEdgeInspection.Contracts.Capture;

/// <summary>
/// 1回の撮影要求を表します。
/// </summary>
public sealed record CaptureRequest(
    string RequestId,
    int CaptureIndex,
    DateTimeOffset RequestedAt);
