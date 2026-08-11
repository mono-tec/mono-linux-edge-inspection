using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// 撮影Runtimeを起動します。
/// </summary>
public interface ICaptureRuntimeLauncher
{
    /// <summary>
    /// 撮影Runtimeを1回起動し、完了を待機します。
    /// </summary>
    /// <param name="cancellationToken">
    /// 起動処理を中止するためのキャンセルトークンです。
    /// </param>
    /// <returns>
    /// 撮影Runtimeの起動結果です。
    /// </returns>
    Task<CaptureRuntimeLaunchResult> LaunchAsync(
        CancellationToken cancellationToken = default);
}