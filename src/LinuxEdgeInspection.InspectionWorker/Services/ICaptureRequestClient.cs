using LinuxEdgeInspection.Contracts.Capture;

namespace LinuxEdgeInspection.InspectionWorker.Services;

public interface ICaptureRequestClient
{
    Task<CaptureResult> SendAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default);
}
