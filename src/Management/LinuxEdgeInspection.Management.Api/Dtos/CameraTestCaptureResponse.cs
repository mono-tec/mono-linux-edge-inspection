namespace LinuxEdgeInspection.Management.Api.Dtos;

public sealed record CameraTestCaptureResponse(
    bool CaptureSucceeded,
    int CaptureIndex,
    string? FilePath,
    string? FileName);
