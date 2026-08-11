namespace LinuxEdgeInspection.Camera.V4L2.Models;

/// <summary>
/// カメラ機能を実行するための環境確認結果を表します。
/// </summary>
/// <param name="DeviceExists">
/// カメラデバイスが存在する場合は<c>true</c>です。
/// </param>
/// <param name="DeviceReadable">
/// カメラデバイスを読み取り可能な場合は<c>true</c>です。
/// </param>
/// <param name="DeviceWritable">
/// カメラデバイスへ書き込み可能な場合は<c>true</c>です。
/// </param>
/// <param name="CommandAvailable">
/// v4l2-ctlコマンドが利用可能な場合は<c>true</c>です。
/// </param>
/// <param name="DevicePath">
/// 確認したカメラデバイスのパスです。
/// </param>
/// <param name="CommandPath">
/// 確認したv4l2-ctlコマンドのパスです。
/// </param>
/// <param name="Message">
/// 環境確認結果の補足メッセージです。
/// </param>
public sealed record CameraEnvironmentStatus(
    bool DeviceExists,
    bool DeviceReadable,
    bool DeviceWritable,
    bool CommandAvailable,
    string DevicePath,
    string CommandPath,
    string? Message)
{
    /// <summary>
    /// カメラ撮影を実行できる環境かどうかを取得します。
    /// </summary>
    public bool IsReady =>
        DeviceExists &&
        DeviceReadable &&
        DeviceWritable &&
        CommandAvailable;
}