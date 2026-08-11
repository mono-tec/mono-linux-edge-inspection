using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// Capture Requestの処理を定義します。
/// </summary>
public interface ICaptureRequestProcessor
{
    /// <summary>
    /// 指定されたCapture Requestを処理します。
    /// </summary>
    /// <param name="request">
    /// 処理対象のCapture Requestです。
    /// </param>
    /// <param name="cancellationToken">
    /// 処理のキャンセルを通知するトークンです。
    /// </param>
    /// <returns>
    /// Runtimeの実行結果を反映したCapture Resultです。
    /// </returns>
    Task<CaptureResult> ProcessAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default);
}