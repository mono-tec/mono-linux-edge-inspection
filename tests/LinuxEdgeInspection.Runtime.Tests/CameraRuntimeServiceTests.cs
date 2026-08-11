using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.Abstractions.Services;
using LinuxEdgeInspection.Runtime.Services;

namespace LinuxEdgeInspection.Runtime.Tests;

public sealed class CameraRuntimeServiceTests
{
    [Fact]
    public async Task RunAsync_WhenCameraIsUnavailable_DoesNotStartOrCapture()
    {
        var cameraService = new FakeCameraService
        {
            StatusResult = CreateUnavailableStatus()
        };

        var service = CreateService(cameraService);

        await service.RunAsync();

        Assert.Equal(
            ["GetStatus"],
            cameraService.Calls);
    }

    [Fact]
    public async Task RunAsync_WhenCameraIsAvailable_ExecutesStartCaptureAndStop()
    {
        var cameraService = new FakeCameraService
        {
            StatusResult = CreateReadyStatus(),
            CaptureResult = CreateSuccessfulCaptureResult()
        };

        var service = CreateService(cameraService);

        await service.RunAsync();

        Assert.Equal(
            [
                "GetStatus",
                "Start",
                "Capture",
                "Stop"
            ],
            cameraService.Calls);
    }

    [Fact]
    public async Task RunAsync_WhenCaptureFails_StillStopsCamera()
    {
        var originalExitCode =
            Environment.ExitCode;

        try
        {
            Environment.ExitCode = 0;

            var cameraService = new FakeCameraService
            {
                StatusResult = CreateReadyStatus(),
                CaptureResult = new CameraCaptureResult(
                    Succeeded: false,
                    FilePath: null,
                    FileSize: 0,
                    CapturedAt: DateTimeOffset.Now,
                    Duration: TimeSpan.FromMilliseconds(100),
                    DevicePath: "/dev/video0",
                    ErrorCode: "CAM-E007",
                    ErrorMessage: "撮影処理に失敗しました。")
            };

            var service = CreateService(cameraService);

            await service.RunAsync();

            Assert.Equal(
                [
                    "GetStatus",
                    "Start",
                    "Capture",
                    "Stop"
                ],
                cameraService.Calls);

            Assert.Equal(
                1,
                Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode =
                originalExitCode;
        }
    }

    [Fact]
    public async Task RunAsync_WhenStartThrows_DoesNotCaptureOrStop()
    {
        var originalExitCode =
            Environment.ExitCode;

        try
        {
            Environment.ExitCode = 0;

            var cameraService = new FakeCameraService
            {
                StatusResult = CreateReadyStatus(),
                StartException =
                    new InvalidOperationException(
                        "カメラを開始できません。")
            };

            var service = CreateService(cameraService);

            await service.RunAsync();

            Assert.Equal(
                [
                    "GetStatus",
                    "Start"
                ],
                cameraService.Calls);

            Assert.Equal(
                1,
                Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode =
                originalExitCode;
        }
    }

    [Fact]
    public async Task RunAsync_WhenCaptureThrows_StillStopsCamera()
    {
        var originalExitCode =
            Environment.ExitCode;

        try
        {
            Environment.ExitCode = 0;

            var cameraService = new FakeCameraService
            {
                StatusResult = CreateReadyStatus(),
                CaptureException =
                    new InvalidOperationException(
                        "撮影中にエラーが発生しました。")
            };

            var service = CreateService(cameraService);

            await service.RunAsync();

            Assert.Equal(
                [
                    "GetStatus",
                    "Start",
                    "Capture",
                    "Stop"
                ],
                cameraService.Calls);

            Assert.Equal(
                1,
                Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode =
                originalExitCode;
        }
    }

    [Fact]
    public async Task RunAsync_PassesCancellationTokenToCameraService()
    {
        var cameraService = new FakeCameraService
        {
            StatusResult = CreateReadyStatus(),
            CaptureResult = CreateSuccessfulCaptureResult()
        };

        var service = CreateService(cameraService);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        await service.RunAsync(
            cancellationTokenSource.Token);

        Assert.Equal(
            cancellationTokenSource.Token,
            cameraService.StatusCancellationToken);

        Assert.Equal(
            cancellationTokenSource.Token,
            cameraService.StartCancellationToken);

        Assert.Equal(
            cancellationTokenSource.Token,
            cameraService.CaptureCancellationToken);
    }

    [Fact]
    public void Constructor_WhenCameraServiceIsNull_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new CameraRuntimeService(
                    null!,
                    CreateOptions()));

        Assert.Equal(
            "cameraService",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenCameraOptionsIsNull_ThrowsArgumentNullException()
    {
        var cameraService =
            new FakeCameraService();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new CameraRuntimeService(
                    cameraService,
                    null!));

        Assert.Equal(
            "cameraOptions",
            exception.ParamName);
    }

    private static CameraRuntimeService CreateService(
        ICameraService cameraService)
    {
        return new CameraRuntimeService(
            cameraService,
            CreateOptions());
    }

    private static CameraOptions CreateOptions()
    {
        return new CameraOptions
        {
            DevicePath = "/dev/video0",
            Width = 640,
            Height = 480,
            PixelFormat = "MJPG",
            FramesPerSecond = 30,
            SkipFrames = 10,
            OutputDirectory = "captures",
            CaptureTimeoutSeconds = 10,
            V4L2CommandPath = "v4l2-ctl"
        };
    }

    private static CameraStatus CreateUnavailableStatus()
    {
        return new CameraStatus(
            IsDeviceConnected: false,
            IsCommandAvailable: false,
            IsReady: false,
            IsCapturing: false,
            DevicePath: "/dev/video0",
            Message: "カメラデバイスが見つかりません。");
    }

    private static CameraStatus CreateReadyStatus()
    {
        return new CameraStatus(
            IsDeviceConnected: true,
            IsCommandAvailable: true,
            IsReady: true,
            IsCapturing: false,
            DevicePath: "/dev/video0",
            Message: "カメラを利用できる環境です。");
    }

    private static CameraCaptureResult
        CreateSuccessfulCaptureResult()
    {
        return new CameraCaptureResult(
            Succeeded: true,
            FilePath: "captures/capture.jpg",
            FileSize: 4,
            CapturedAt: DateTimeOffset.Now,
            Duration: TimeSpan.FromMilliseconds(100),
            DevicePath: "/dev/video0",
            ErrorCode: null,
            ErrorMessage: null);
    }

    private sealed class FakeCameraService
        : ICameraService
    {
        public CameraStatus StatusResult { get; set; } =
            CreateUnavailableStatus();

        public CameraCaptureResult CaptureResult { get; set; } =
            CreateSuccessfulCaptureResult();

        public Exception? StartException { get; set; }

        public Exception? CaptureException { get; set; }

        public Exception? StopException { get; set; }

        public List<string> Calls { get; } = [];

        public CancellationToken
            StatusCancellationToken
        { get; private set; }

        public CancellationToken
            StartCancellationToken
        { get; private set; }

        public CancellationToken
            CaptureCancellationToken
        { get; private set; }

        public CancellationToken
            StopCancellationToken
        { get; private set; }

        public Task<CameraStatus> GetStatusAsync(
            CancellationToken cancellationToken = default)
        {
            Calls.Add("GetStatus");

            StatusCancellationToken =
                cancellationToken;

            return Task.FromResult(
                StatusResult);
        }

        public Task StartAsync(
            CancellationToken cancellationToken = default)
        {
            Calls.Add("Start");

            StartCancellationToken =
                cancellationToken;

            if (StartException is not null)
            {
                throw StartException;
            }

            return Task.CompletedTask;
        }

        public Task<CameraCaptureResult> CaptureAsync(
            CameraCaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("Capture");

            CaptureCancellationToken =
                cancellationToken;

            if (CaptureException is not null)
            {
                throw CaptureException;
            }

            return Task.FromResult(
                CaptureResult);
        }

        public Task StopAsync(
            CancellationToken cancellationToken = default)
        {
            Calls.Add("Stop");

            StopCancellationToken =
                cancellationToken;

            if (StopException is not null)
            {
                throw StopException;
            }

            return Task.CompletedTask;
        }
    }
}