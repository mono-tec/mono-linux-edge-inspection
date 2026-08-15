using LinuxEdgeInspection.CaptureRequestListener.Models;
using System.Threading.Channels;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// Channelを使用してCapture RequestをFIFOで保持します。
/// </summary>
public sealed class CaptureRequestQueue : ICaptureRequestQueue
{
    private readonly Channel<CaptureRequestQueueItem> _channel =
        Channel.CreateUnbounded<CaptureRequestQueueItem>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

    public ValueTask EnqueueAsync(
        CaptureRequestQueueItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public ValueTask<CaptureRequestQueueItem> DequeueAsync(
        CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAsync(cancellationToken);
}
