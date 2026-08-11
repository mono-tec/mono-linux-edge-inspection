using System.ComponentModel.DataAnnotations;

namespace LinuxEdgeInspection.Camera.Abstractions.Models;

/// <summary>
/// カメラ機能の設定を表します。
/// </summary>
public sealed class CameraOptions
{
    /// <summary>
    /// 設定ファイル内のセクション名です。
    /// </summary>
    public const string SectionName = "Camera";

    /// <summary>
    /// 使用するカメラデバイスのパスです。
    /// </summary>
    [Required]
    public string DevicePath { get; set; } = "/dev/video0";

    /// <summary>
    /// 撮影画像の幅です。
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Width { get; set; } = 640;

    /// <summary>
    /// 撮影画像の高さです。
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Height { get; set; } = 480;

    /// <summary>
    /// V4L2で使用するピクセルフォーマットです。
    /// </summary>
    [Required]
    public string PixelFormat { get; set; } = "MJPG";

    /// <summary>
    /// 撮影時のフレームレートです。
    /// </summary>
    [Range(1, int.MaxValue)]
    public int FramesPerSecond { get; set; } = 30;

    /// <summary>
    /// 撮影前に読み飛ばすフレーム数です。
    /// </summary>
    [Range(0, int.MaxValue)]
    public int SkipFrames { get; set; } = 10;

    /// <summary>
    /// 撮影画像の保存先ディレクトリです。
    /// </summary>
    [Required]
    public string OutputDirectory { get; set; }
        = "/var/lib/linux-edge-inspection/camera";

    /// <summary>
    /// 撮影処理のタイムアウト時間です。
    /// </summary>
    [Range(1, int.MaxValue)]
    public int CaptureTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// カメラ操作に使用するコマンドのパスです。
    /// </summary>
    [Required]
    public string V4L2CommandPath { get; set; } = "v4l2-ctl";
}