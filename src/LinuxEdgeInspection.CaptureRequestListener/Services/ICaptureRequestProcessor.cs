using LinuxEdgeInspection.Contracts.Capture;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

public interface ICaptureRequestProcessor
{
    Task<CaptureResult> ProcessAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default);
}
