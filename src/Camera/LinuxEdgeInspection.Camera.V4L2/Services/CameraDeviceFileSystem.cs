using LinuxEdgeInspection.Camera.V4L2.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// 実際のファイルシステムを使用して、
/// カメラデバイスファイルへのアクセス状態を確認します。
/// </summary>
public sealed class CameraDeviceFileSystem : ICameraDeviceFileSystem
{
    /// <inheritdoc />
    public CameraDeviceAccessStatus CheckAccess(string devicePath)
    {
        if (string.IsNullOrWhiteSpace(devicePath))
        {
            throw new ArgumentException(
                "カメラデバイスのパスを指定してください。",
                nameof(devicePath));
        }

        if (!File.Exists(devicePath))
        {
            return new CameraDeviceAccessStatus(
                Exists: false,
                Readable: false,
                Writable: false);
        }

        var readable = CanOpen(
            devicePath,
            FileAccess.Read);

        var writable = CanOpen(
            devicePath,
            FileAccess.Write);

        return new CameraDeviceAccessStatus(
            Exists: true,
            Readable: readable,
            Writable: writable);
    }

    private static bool CanOpen(
        string devicePath,
        FileAccess access)
    {
        try
        {
            using var stream = new FileStream(
                devicePath,
                FileMode.Open,
                access,
                FileShare.ReadWrite);

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}