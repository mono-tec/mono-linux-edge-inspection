namespace LinuxEdgeInspection.Management.Api.Services;

public enum CaptureImageOpenStatus
{
    Success,
    InvalidFileName,
    NotFound,
    SymbolicLinkRejected
}

public sealed record CaptureImageOpenResult(
    CaptureImageOpenStatus Status,
    Stream? Stream = null);
