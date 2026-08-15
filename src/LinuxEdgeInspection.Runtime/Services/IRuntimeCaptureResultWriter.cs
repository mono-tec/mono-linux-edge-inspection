using LinuxEdgeInspection.Runtime.Models;

namespace LinuxEdgeInspection.Runtime.Services;

/// <summary>
/// 撮影Runtimeの実行結果を保存します。
/// </summary>
public interface IRuntimeCaptureResultWriter
{
    /// <summary>
    /// 撮影Runtimeの実行結果を保存します。
    /// </summary>
    /// <param name="result">
    /// 保存する撮影Runtimeの実行結果です。
    /// </param>
    /// <param name="cancellationToken">
    /// キャンセル通知を受け取るトークンです。
    /// </param>
    Task WriteAsync(
        RuntimeCaptureResult result,
        CancellationToken cancellationToken = default);
}