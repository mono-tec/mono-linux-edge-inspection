namespace LinuxEdgeInspection.Camera.Abstractions.Models;

/// <summary>
/// カメラへ静止画取得を要求する際の条件を表します。
/// </summary>
/// <param name="OutputPath">撮影した画像の出力先パス。</param>
/// <param name="Width">撮影画像の幅（ピクセル）。</param>
/// <param name="Height">撮影画像の高さ（ピクセル）。</param>
/// <param name="PixelFormat">撮影時に使用するピクセルフォーマット。</param>
/// <param name="FramesPerSecond">撮影時に使用するフレームレート。</param>
/// <param name="SkipFrames">
/// 撮影開始後に読み飛ばすフレーム数。
/// カメラ起動直後の不安定なフレームを除外する場合などに使用します。
/// </param>
public sealed record CameraCaptureRequest(
    string? OutputPath = null,
    int? Width = null,
    int? Height = null,
    string? PixelFormat = null,
    int? FramesPerSecond = null,
    int? SkipFrames = null);