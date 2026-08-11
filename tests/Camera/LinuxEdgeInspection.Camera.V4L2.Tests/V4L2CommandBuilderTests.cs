
using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.V4L2.Services;

namespace LinuxEdgeInspection.Camera.V4L2.Tests;

public sealed class V4L2CommandBuilderTests
{
    private readonly V4L2CommandBuilder _builder = new();

    [Fact]
    public void BuildCaptureArguments_WithDefaultOptions_ReturnsExpectedArguments()
    {
        var options = new CameraOptions();
        const string outputPath = "/tmp/capture.jpg";

        var arguments = _builder.BuildCaptureArguments(
            options,
            outputPath);

        Assert.Equal(
            [
                "--device=/dev/video0",
                "--set-fmt-video=width=640,height=480,pixelformat=MJPG",
                "--set-parm=30",
                "--stream-mmap",
                "--stream-skip=10",
                "--stream-count=1",
                "--stream-to=/tmp/capture.jpg"
            ],
            arguments);
    }

    [Fact]
    public void BuildCaptureArguments_WithCustomOptions_ReflectsSpecifiedValues()
    {
        var options = new CameraOptions
        {
            DevicePath = "/dev/video2",
            Width = 1280,
            Height = 960,
            PixelFormat = "YUYV",
            FramesPerSecond = 15,
            SkipFrames = 5
        };

        const string outputPath = "/var/tmp/custom.jpg";

        var arguments = _builder.BuildCaptureArguments(
            options,
            outputPath);

        Assert.Contains("--device=/dev/video2", arguments);
        Assert.Contains(
            "--set-fmt-video=width=1280,height=960,pixelformat=YUYV",
            arguments);
        Assert.Contains("--set-parm=15", arguments);
        Assert.Contains("--stream-skip=5", arguments);
        Assert.Contains("--stream-to=/var/tmp/custom.jpg", arguments);
    }

    [Fact]
    public void BuildCaptureArguments_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => _builder.BuildCaptureArguments(
                null!,
                "/tmp/capture.jpg"));

        Assert.Equal("options", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void BuildCaptureArguments_WhenOutputPathIsEmpty_ThrowsArgumentException(
        string outputPath)
    {
        var options = new CameraOptions();

        var exception = Assert.Throws<ArgumentException>(
            () => _builder.BuildCaptureArguments(
                options,
                outputPath));

        Assert.Equal("outputPath", exception.ParamName);
    }
}