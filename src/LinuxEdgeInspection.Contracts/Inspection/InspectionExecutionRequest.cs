namespace LinuxEdgeInspection.Contracts.Inspection;

/// <summary>
/// InspectionWorkerへ1回のInspection Pipeline実行を要求します。
/// </summary>
/// <param name="RequestId">Inspection要求を一意に識別するID。</param>
/// <param name="CaptureIndex">今回のInspectionで実行する撮像番号。</param>
/// <param name="RequestedAt">Inspection実行を要求した日時。</param>
public sealed record InspectionExecutionRequest(
    string RequestId,
    int CaptureIndex,
    DateTimeOffset RequestedAt);