namespace LinuxEdgeInspection.Camera.Abstractions.Models;

/// <summary>
/// カメラへ静止画取得を要求する際の条件を表します。
/// </summary>
public sealed record CameraCaptureRequest(
    string? OutputPath = null,
    int? Width = null,
    int? Height = null,
    string? PixelFormat = null,
    int? FramesPerSecond = null,
    int? SkipFrames = null);