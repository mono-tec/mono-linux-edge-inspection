namespace LinuxEdgeInspection.Contracts.Capture;

/// <summary>
/// 1回の撮影要求を表します。
/// </summary>
/// <param name="RequestId">
/// 撮影要求を識別するためのID。
/// 同じInspectionに属するCaptureでは、同一のRequestIdを使用します。
/// </param>
/// <param name="CaptureIndex">
/// 1つのInspection内における撮影の識別番号。
/// 複数回撮影する場合に、各Captureを区別するために使用します。
/// </param>
/// <param name="RequestedAt">
/// 撮影要求が生成された日時。
/// </param>
public sealed record CaptureRequest(
    string RequestId,
    int CaptureIndex,
    DateTimeOffset RequestedAt);