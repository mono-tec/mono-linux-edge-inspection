using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.V4L2.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// カメラ機能を実行するためのLinux環境を確認する機能を定義します。
/// </summary>
public interface ICameraEnvironmentService
{
    /// <summary>
    /// カメラデバイスとv4l2-ctlコマンドの利用可否を確認します。
    /// </summary>
    /// <param name="options">
    /// 確認対象となるカメラ設定です。
    /// </param>
    /// <param name="cancellationToken">
    /// 処理のキャンセルを通知するトークンです。
    /// </param>
    /// <returns>
    /// カメラ環境の確認結果です。
    /// </returns>
    Task<CameraEnvironmentStatus> CheckAsync(
        CameraOptions options,
        CancellationToken cancellationToken = default);
}