namespace LinuxEdgeInspection.Camera.Abstractions.Models;

/// <summary>
/// カメラ機能の現在状態を表します。
/// </summary>
/// <param name="IsDeviceConnected">
/// カメラデバイスが接続されている場合は<c>true</c>。
/// </param>
/// <param name="IsCommandAvailable">
/// カメラ操作に必要な外部コマンドが利用できる場合は<c>true</c>。
/// </param>
/// <param name="IsReady">
/// カメラが撮影可能な状態の場合は<c>true</c>。
/// </param>
/// <param name="IsCapturing">
/// 撮影処理を実行中の場合は<c>true</c>。
/// </param>
/// <param name="DevicePath">
/// カメラデバイスのパス。
/// </param>
/// <param name="Message">
/// 状態に関する補足メッセージ。
/// </param>
public sealed record CameraStatus(
    bool IsDeviceConnected,
    bool IsCommandAvailable,
    bool IsReady,
    bool IsCapturing,
    string DevicePath,
    string? Message);