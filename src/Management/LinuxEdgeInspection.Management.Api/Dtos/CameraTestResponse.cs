namespace LinuxEdgeInspection.Management.Api.Dtos;

public sealed record CameraTestResponse(
    string RequestId,
    IReadOnlyList<CameraTestCaptureResponse> Captures,
    bool? PreprocessSucceeded,
    bool? AnalysisSucceeded,
    string? Judgement,
    string? Label,
    string? ErrorCode,
    string? ErrorMessage);
