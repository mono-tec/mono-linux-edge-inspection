using LinuxEdgeInspection.Contracts.Capture;

namespace LinuxEdgeInspection.CaptureRequestListener.Models;

/// <summary>
/// Queue内のCaptureRequestと、その処理完了通知を関連付けます。
/// </summary>
public sealed class CaptureRequestQueueItem
{
    private readonly TaskCompletionSource<CaptureResult> _completionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CaptureRequestQueueItem(CaptureRequest request)
    {
        Request = request
            ?? throw new ArgumentNullException(nameof(request));
    }

    public CaptureRequest Request { get; }

    public Task<CaptureResult> Completion => _completionSource.Task;

    public bool TrySetResult(CaptureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return _completionSource.TrySetResult(result);
    }

    public bool TrySetCanceled(CancellationToken cancellationToken) =>
        _completionSource.TrySetCanceled(cancellationToken);
}
