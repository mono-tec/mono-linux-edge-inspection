namespace LinuxEdgeInspection.Camera.Abstractions.Models;

/// <summary>
/// カメラによる静止画取得処理の結果を表します。
/// </summary>
public sealed record CameraCaptureResult(
    bool Succeeded,
    string? FilePath,
    long FileSize,
    DateTimeOffset CapturedAt,
    TimeSpan Duration,
    string DevicePath,
    string? ErrorCode,
    string? ErrorMessage);