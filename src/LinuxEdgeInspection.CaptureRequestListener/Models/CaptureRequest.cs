namespace LinuxEdgeInspection.CaptureRequestListener.Models;

/// <summary>
/// PLCから受信した撮影要求を表します。
/// </summary>
/// <param name="RequestId">
/// 撮影要求を識別する番号です。
/// </param>
/// <param name="RequestedAt">
/// 撮影要求を受信した日時です。
/// </param>
public sealed record CaptureRequest(
    long RequestId,
    DateTimeOffset RequestedAt);