namespace LinuxEdgeInspection.Runtime.Services;

/// <summary>
/// カメラを使用したRuntime処理を実行します。
/// </summary>
public interface ICameraRuntimeService
{
    /// <summary>
    /// カメラ環境を確認し、利用可能な場合は撮影処理を実行します。
    /// </summary>
    /// <param name="cancellationToken">
    /// 処理のキャンセルトークンです。
    /// </param>
    Task RunAsync(
        CancellationToken cancellationToken = default);
}