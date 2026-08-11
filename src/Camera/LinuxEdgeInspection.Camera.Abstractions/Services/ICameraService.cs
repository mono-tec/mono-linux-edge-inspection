using LinuxEdgeInspection.Camera.Abstractions.Models;

namespace LinuxEdgeInspection.Camera.Abstractions.Services;

/// <summary>
/// カメラ機能の共通操作を定義します。
/// </summary>
public interface ICameraService
{
    /// <summary>
    /// カメラデバイスの現在状態を取得します。
    /// </summary>
    /// <param name="cancellationToken">
    /// 処理のキャンセルを通知するトークンです。
    /// </param>
    /// <returns>
    /// カメラの接続状態、利用可否、撮影状態を表す結果です。
    /// </returns>
    Task<CameraStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// カメラ機能の利用を開始します。
    /// </summary>
    /// <param name="cancellationToken">
    /// 処理のキャンセルを通知するトークンです。
    /// </param>
    Task StartAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定された条件で静止画を1枚取得します。
    /// </summary>
    /// <param name="request">
    /// 出力先や撮影条件を含む撮影要求です。
    /// </param>
    /// <param name="cancellationToken">
    /// 処理のキャンセルを通知するトークンです。
    /// </param>
    /// <returns>
    /// 撮影結果、保存先、ファイルサイズ、エラー情報などを含む結果です。
    /// </returns>
    Task<CameraCaptureResult> CaptureAsync(
        CameraCaptureRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// カメラ機能の利用を終了します。
    /// </summary>
    /// <param name="cancellationToken">
    /// 処理のキャンセルを通知するトークンです。
    /// </param>
    Task StopAsync(
        CancellationToken cancellationToken = default);
}
