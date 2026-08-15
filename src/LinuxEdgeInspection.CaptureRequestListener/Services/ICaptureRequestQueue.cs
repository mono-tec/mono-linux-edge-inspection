using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// Capture RequestをFIFO順に保持します。
/// </summary>
public interface ICaptureRequestQueue
{
    ValueTask EnqueueAsync(
        CaptureRequestQueueItem item,
        CancellationToken cancellationToken = default);

    ValueTask<CaptureRequestQueueItem> DequeueAsync(
        CancellationToken cancellationToken = default);
}
