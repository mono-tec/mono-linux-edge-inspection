namespace LinuxEdgeInspection.Camera.V4L2.Models;

/// <summary>
/// カメラデバイスファイルへのアクセス確認結果を表します。
/// </summary>
/// <param name="Exists">
/// デバイスファイルが存在する場合は<c>true</c>です。
/// </param>
/// <param name="Readable">
/// デバイスファイルを読み取り可能な場合は<c>true</c>です。
/// </param>
/// <param name="Writable">
/// デバイスファイルへ書き込み可能な場合は<c>true</c>です。
/// </param>
public sealed record CameraDeviceAccessStatus(
    bool Exists,
    bool Readable,
    bool Writable);