using LinuxEdgeInspection.CaptureRequestListener.Models;
using System.Threading.Channels;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// Channelを使用してPLC撮影要求をFIFOで保持します。
/// </summary>
public sealed class CaptureRequestQueue
    : ICaptureRequestQueue
{
    private readonly Channel<CaptureRequest> _channel;

    /// <summary>
    /// <see cref="CaptureRequestQueue"/>を初期化します。
    /// </summary>
    public CaptureRequestQueue()
    {
        _channel =
            Channel.CreateUnbounded<CaptureRequest>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _channel.Writer.WriteAsync(
            request,
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<CaptureRequest> DequeueAsync(
        CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(
            cancellationToken);
    }
}