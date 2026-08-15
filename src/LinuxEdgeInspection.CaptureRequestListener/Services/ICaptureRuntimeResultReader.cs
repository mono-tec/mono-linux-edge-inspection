using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// 撮影Runtimeが出力した実行結果を読み込みます。
/// </summary>
public interface ICaptureRuntimeResultReader
{
    /// <summary>
    /// 撮影Runtimeの実行結果を読み込みます。
    /// </summary>
    /// <param name="cancellationToken">
    /// キャンセル通知を受け取るトークンです。
    /// </param>
    /// <returns>
    /// 撮影Runtimeの実行結果です。
    /// </returns>
    Task<CaptureRuntimeResult> ReadAsync(
        CancellationToken cancellationToken = default);
}