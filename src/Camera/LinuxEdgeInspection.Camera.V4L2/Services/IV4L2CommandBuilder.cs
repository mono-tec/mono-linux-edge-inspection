using LinuxEdgeInspection.Camera.Abstractions.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// v4l2-ctlへ渡す引数を生成する機能を定義します。
/// </summary>
public interface IV4L2CommandBuilder
{
    /// <summary>
    /// 静止画取得に使用するv4l2-ctlの引数を生成します。
    /// </summary>
    /// <param name="options">
    /// カメラの撮影設定です。
    /// </param>
    /// <param name="outputPath">
    /// 撮影画像の出力先パスです。
    /// </param>
    /// <returns>
    /// ProcessStartInfo.ArgumentListへ追加できる引数一覧です。
    /// </returns>
    IReadOnlyList<string> BuildCaptureArguments(
        CameraOptions options,
        string outputPath);
}