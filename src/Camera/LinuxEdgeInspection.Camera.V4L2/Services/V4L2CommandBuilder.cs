using LinuxEdgeInspection.Camera.Abstractions.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// CameraOptionsからv4l2-ctlの静止画取得用引数を生成します。
/// </summary>
public sealed class V4L2CommandBuilder : IV4L2CommandBuilder
{
    /// <inheritdoc />
    public IReadOnlyList<string> BuildCaptureArguments(
        CameraOptions options,
        string outputPath)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "画像の出力先パスを指定してください。",
                nameof(outputPath));
        }

        return
        [
            $"--device={options.DevicePath}",
            $"--set-fmt-video=width={options.Width}," +
            $"height={options.Height}," +
            $"pixelformat={options.PixelFormat}",
            $"--set-parm={options.FramesPerSecond}",
            "--stream-mmap",
            $"--stream-skip={options.SkipFrames}",
            "--stream-count=1",
            $"--stream-to={outputPath}"
        ];
    }
}