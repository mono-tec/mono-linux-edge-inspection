using LinuxEdgeInspection.Camera.V4L2.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// カメラデバイスファイルの存在とアクセス権限を確認する機能を定義します。
/// </summary>
public interface ICameraDeviceFileSystem
{
    /// <summary>
    /// 指定されたカメラデバイスファイルの状態を確認します。
    /// </summary>
    /// <param name="devicePath">
    /// 確認対象のデバイスパスです。
    /// </param>
    /// <returns>
    /// デバイスファイルの存在、読み取り、書き込み可否です。
    /// </returns>
    CameraDeviceAccessStatus CheckAccess(string devicePath);
}