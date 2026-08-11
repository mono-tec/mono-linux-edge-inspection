using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// PLCから受信した撮影要求を順番に保持します。
/// </summary>
public interface ICaptureRequestQueue
{
    /// <summary>
    /// 撮影要求をQueueへ追加します。
    /// </summary>
    /// <param name="request">
    /// 追加する撮影要求です。
    /// </param>
    /// <param name="cancellationToken">
    /// 追加処理を中止するためのキャンセルトークンです。
    /// </param>
    ValueTask EnqueueAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 次の撮影要求をQueueから取得します。
    /// </summary>
    /// <param name="cancellationToken">
    /// 待機処理を中止するためのキャンセルトークンです。
    /// </param>
    /// <returns>
    /// Queueの先頭にある撮影要求です。
    /// </returns>
    ValueTask<CaptureRequest> DequeueAsync(
        CancellationToken cancellationToken = default);
}