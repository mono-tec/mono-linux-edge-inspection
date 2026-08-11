using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.V4L2.Models;
using LinuxEdgeInspection.Camera.V4L2.Services;

namespace LinuxEdgeInspection.Camera.V4L2.Tests;

public sealed class V4L2CameraServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly CameraOptions _options;

    public V4L2CameraServiceTests()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "KakipEdgePlatform.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_testDirectory);

        _options = new CameraOptions
        {
            DevicePath = "/dev/video0",
            Width = 640,
            Height = 480,
            PixelFormat = "MJPG",
            FramesPerSecond = 30,
            SkipFrames = 10,
            OutputDirectory = _testDirectory,
            CaptureTimeoutSeconds = 10,
            V4L2CommandPath = "v4l2-ctl"
        };
    }

    [Fact]
    public async Task StartAsync_WhenEnvironmentIsReady_ChangesStateToReady()
    {
        var stateManager = new CameraStateManager();

        using var service = CreateService(
            stateManager: stateManager);

        await service.StartAsync();

        Assert.Equal(
            CameraState.Ready,
            stateManager.CurrentState);
    }

    [Fact]
    public async Task StartAsync_WhenEnvironmentIsNotReady_ChangesStateToFaulted()
    {
        var stateManager = new CameraStateManager();

        var environmentService =
            new FakeCameraEnvironmentService(
                new CameraEnvironmentStatus(
                    DeviceExists: false,
                    DeviceReadable: false,
                    DeviceWritable: false,
                    CommandAvailable: true,
                    DevicePath: _options.DevicePath,
                    CommandPath: _options.V4L2CommandPath,
                    Message: "カメラデバイスが見つかりません。"));

        using var service = CreateService(
            stateManager: stateManager,
            environmentService: environmentService);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync());

        Assert.Equal(
            CameraState.Faulted,
            stateManager.CurrentState);

        Assert.Contains(
            "カメラデバイスが見つかりません",
            exception.Message);
    }

    [Fact]
    public async Task GetStatusAsync_WhenStarted_ReturnsReadyStatus()
    {
        var stateManager = new CameraStateManager();

        using var service = CreateService(
            stateManager: stateManager);

        await service.StartAsync();

        var status = await service.GetStatusAsync();

        Assert.True(status.IsDeviceConnected);
        Assert.True(status.IsCommandAvailable);
        Assert.True(status.IsReady);
        Assert.False(status.IsCapturing);
        Assert.Equal(
            _options.DevicePath,
            status.DevicePath);
    }

    [Fact]
    public async Task CaptureAsync_WhenServiceIsNotStarted_ThrowsInvalidOperationException()
    {
        using var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CaptureAsync(
                new CameraCaptureRequest()));

        Assert.Contains(
            "StartAsync",
            exception.Message);
    }

    [Fact]
    public async Task CaptureAsync_WhenProcessSucceeds_ReturnsSuccessfulResult()
    {
        var processRunner =
            new FakeCameraProcessRunner(
                CreateSuccessfulProcessResult(),
                createCaptureFile: true);

        var commandBuilder =
            new FakeV4L2CommandBuilder();

        using var service = CreateService(
            processRunner: processRunner,
            commandBuilder: commandBuilder);

        await service.StartAsync();

        var result = await service.CaptureAsync(
            new CameraCaptureRequest());

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.FilePath);
        Assert.True(File.Exists(result.FilePath));
        Assert.True(result.FileSize > 0);
        Assert.Equal(
            _options.DevicePath,
            result.DevicePath);

        Assert.NotNull(processRunner.LastRequest);
        Assert.NotNull(commandBuilder.LastOptions);
        Assert.NotNull(commandBuilder.LastOutputPath);

        Assert.EndsWith(
            ".jpg.tmp",
            commandBuilder.LastOutputPath);
    }

    [Fact]
    public async Task CaptureAsync_WithCustomRequest_AppliesSpecifiedValues()
    {
        var processRunner =
            new FakeCameraProcessRunner(
                CreateSuccessfulProcessResult(),
                createCaptureFile: true);

        var commandBuilder =
            new FakeV4L2CommandBuilder();

        using var service = CreateService(
            processRunner: processRunner,
            commandBuilder: commandBuilder);

        await service.StartAsync();

        var outputPath = Path.Combine(
            _testDirectory,
            "custom-capture.jpg");

        var result = await service.CaptureAsync(
            new CameraCaptureRequest(
                OutputPath: outputPath,
                Width: 1280,
                Height: 960,
                PixelFormat: "YUYV",
                FramesPerSecond: 15,
                SkipFrames: 5));

        Assert.True(result.Succeeded);
        Assert.Equal(
            outputPath,
            result.FilePath);

        Assert.NotNull(commandBuilder.LastOptions);

        Assert.Equal(
            1280,
            commandBuilder.LastOptions.Width);

        Assert.Equal(
            960,
            commandBuilder.LastOptions.Height);

        Assert.Equal(
            "YUYV",
            commandBuilder.LastOptions.PixelFormat);

        Assert.Equal(
            15,
            commandBuilder.LastOptions.FramesPerSecond);

        Assert.Equal(
            5,
            commandBuilder.LastOptions.SkipFrames);
    }

    [Fact]
    public async Task CaptureAsync_WhenProcessTimesOut_ReturnsTimeoutError()
    {
        var processRunner =
            new FakeCameraProcessRunner(
                new ProcessExecutionResult(
                    ExitCode: null,
                    StandardOutput: string.Empty,
                    StandardError: string.Empty,
                    Duration: TimeSpan.FromSeconds(10),
                    TimedOut: true,
                    Cancelled: false));

        using var service = CreateService(
            processRunner: processRunner);

        await service.StartAsync();

        var result = await service.CaptureAsync(
            new CameraCaptureRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(
            "CAM-E006",
            result.ErrorCode);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));
    }

    [Fact]
    public async Task CaptureAsync_WhenProcessIsCancelled_ReturnsCancelledError()
    {
        var processRunner =
            new FakeCameraProcessRunner(
                new ProcessExecutionResult(
                    ExitCode: null,
                    StandardOutput: string.Empty,
                    StandardError: string.Empty,
                    Duration: TimeSpan.FromMilliseconds(100),
                    TimedOut: false,
                    Cancelled: true));

        using var service = CreateService(
            processRunner: processRunner);

        await service.StartAsync();

        var result = await service.CaptureAsync(
            new CameraCaptureRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(
            "CAM-E010",
            result.ErrorCode);
    }

    [Fact]
    public async Task CaptureAsync_WhenProcessFails_ReturnsProcessError()
    {
        var processRunner =
            new FakeCameraProcessRunner(
                new ProcessExecutionResult(
                    ExitCode: 1,
                    StandardOutput: string.Empty,
                    StandardError:
                        "Cannot open device /dev/video0",
                    Duration: TimeSpan.FromMilliseconds(100),
                    TimedOut: false,
                    Cancelled: false));

        using var service = CreateService(
            processRunner: processRunner);

        await service.StartAsync();

        var result = await service.CaptureAsync(
            new CameraCaptureRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(
            "CAM-E007",
            result.ErrorCode);

        Assert.Contains(
            "Cannot open device",
            result.ErrorMessage);
    }

    [Fact]
    public async Task CaptureAsync_WhenCaptureFileIsNotCreated_ReturnsInvalidFileError()
    {
        var processRunner =
            new FakeCameraProcessRunner(
                CreateSuccessfulProcessResult(),
                createCaptureFile: false);

        using var service = CreateService(
            processRunner: processRunner);

        await service.StartAsync();

        var result = await service.CaptureAsync(
            new CameraCaptureRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(
            "CAM-E009",
            result.ErrorCode);
    }

    [Fact]
    public async Task CaptureAsync_AfterCompletion_ReturnsStateToReady()
    {
        var stateManager = new CameraStateManager();

        var processRunner =
            new FakeCameraProcessRunner(
                CreateSuccessfulProcessResult(),
                createCaptureFile: true);

        using var service = CreateService(
            stateManager: stateManager,
            processRunner: processRunner);

        await service.StartAsync();

        await service.CaptureAsync(
            new CameraCaptureRequest());

        Assert.Equal(
            CameraState.Ready,
            stateManager.CurrentState);
    }

    [Fact]
    public async Task StopAsync_WhenReady_ChangesStateToStopped()
    {
        var stateManager = new CameraStateManager();

        using var service = CreateService(
            stateManager: stateManager);

        await service.StartAsync();
        await service.StopAsync();

        Assert.Equal(
            CameraState.Stopped,
            stateManager.CurrentState);
    }

    [Fact]
    public async Task CaptureAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        using var service = CreateService();

        await service.StartAsync();

        var exception =
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.CaptureAsync(null!));

        Assert.Equal(
            "request",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new V4L2CameraService(
                null!,
                CreateReadyEnvironmentService(),
                new FakeV4L2CommandBuilder(),
                new CameraFileService(),
                new FakeCameraProcessRunner(
                    CreateSuccessfulProcessResult()),
                new CameraStateManager()));

        Assert.Equal(
            "options",
            exception.ParamName);
    }

    [Fact]
    public async Task CaptureAsync_WhenAnotherCaptureIsRunning_ReturnsCameraBusyError()
    {
        var processRunner =
            new BlockingCameraProcessRunner();

        using var service = CreateService(
            processRunner: processRunner);

        await service.StartAsync();

        var firstCaptureTask = service.CaptureAsync(
            new CameraCaptureRequest());

        await processRunner.WaitUntilStartedAsync();

        var secondResult = await service.CaptureAsync(
            new CameraCaptureRequest());

        Assert.False(secondResult.Succeeded);
        Assert.Equal(
            "CAM-E005",
            secondResult.ErrorCode);

        Assert.Contains(
            "撮影処理中",
            secondResult.ErrorMessage);

        processRunner.Complete();

        var firstResult = await firstCaptureTask;

        Assert.True(firstResult.Succeeded);
    }



    private V4L2CameraService CreateService(
        ICameraStateManager? stateManager = null,
        ICameraEnvironmentService? environmentService = null,
        IV4L2CommandBuilder? commandBuilder = null,
        ICameraProcessRunner? processRunner = null)
    {
        return new V4L2CameraService(
            _options,
            environmentService
                ?? CreateReadyEnvironmentService(),
            commandBuilder
                ?? new FakeV4L2CommandBuilder(),
            new CameraFileService(),
            processRunner
                ?? new FakeCameraProcessRunner(
                    CreateSuccessfulProcessResult(),
                    createCaptureFile: true),
            stateManager
                ?? new CameraStateManager());
    }

    private ICameraEnvironmentService
        CreateReadyEnvironmentService()
    {
        return new FakeCameraEnvironmentService(
            new CameraEnvironmentStatus(
                DeviceExists: true,
                DeviceReadable: true,
                DeviceWritable: true,
                CommandAvailable: true,
                DevicePath: _options.DevicePath,
                CommandPath: _options.V4L2CommandPath,
                Message: "カメラを利用できる環境です。"));
    }

    private static ProcessExecutionResult
        CreateSuccessfulProcessResult()
    {
        return new ProcessExecutionResult(
            ExitCode: 0,
            StandardOutput: string.Empty,
            StandardError: string.Empty,
            Duration: TimeSpan.FromMilliseconds(100),
            TimedOut: false,
            Cancelled: false);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(
                _testDirectory,
                recursive: true);
        }
    }

    private sealed class FakeCameraEnvironmentService
        : ICameraEnvironmentService
    {
        private readonly CameraEnvironmentStatus _status;

        public FakeCameraEnvironmentService(
            CameraEnvironmentStatus status)
        {
            _status = status;
        }

        public Task<CameraEnvironmentStatus> CheckAsync(
            CameraOptions options,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(_status);
        }
    }

    private sealed class FakeV4L2CommandBuilder
        : IV4L2CommandBuilder
    {
        public CameraOptions? LastOptions { get; private set; }

        public string? LastOutputPath { get; private set; }

        public IReadOnlyList<string> BuildCaptureArguments(
            CameraOptions options,
            string outputPath)
        {
            LastOptions = options;
            LastOutputPath = outputPath;

            return
            [
                $"--device={options.DevicePath}",
                $"--stream-to={outputPath}"
            ];
        }
    }

    private sealed class FakeCameraProcessRunner
        : ICameraProcessRunner
    {
        private readonly ProcessExecutionResult _result;
        private readonly bool _createCaptureFile;

        public FakeCameraProcessRunner(
            ProcessExecutionResult result,
            bool createCaptureFile = false)
        {
            _result = result;
            _createCaptureFile = createCaptureFile;
        }

        public ProcessExecutionRequest? LastRequest { get; private set; }

        public async Task<ProcessExecutionResult> ExecuteAsync(
            ProcessExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastRequest = request;

            if (_createCaptureFile)
            {
                var outputArgument =
                    request.Arguments.FirstOrDefault(
                        argument =>
                            argument.StartsWith(
                                "--stream-to=",
                                StringComparison.Ordinal));

                if (outputArgument is not null)
                {
                    var outputPath =
                        outputArgument[
                            "--stream-to=".Length..];

                    var directory =
                        Path.GetDirectoryName(outputPath);

                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await File.WriteAllBytesAsync(
                        outputPath,
                        [0xFF, 0xD8, 0xFF, 0xD9],
                        cancellationToken);
                }
            }

            return _result;
        }

    }

    private sealed class BlockingCameraProcessRunner
    : ICameraProcessRunner
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitUntilStartedAsync()
        {
            return _started.Task;
        }

        public void Complete()
        {
            _completion.TrySetResult();
        }

        public async Task<ProcessExecutionResult> ExecuteAsync(
            ProcessExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();

            await _completion.Task.WaitAsync(
                cancellationToken);

            var outputArgument =
                request.Arguments.FirstOrDefault(
                    argument =>
                        argument.StartsWith(
                            "--stream-to=",
                            StringComparison.Ordinal));

            if (outputArgument is not null)
            {
                var outputPath =
                    outputArgument["--stream-to=".Length..];

                var directory =
                    Path.GetDirectoryName(outputPath);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(
                    outputPath,
                    [0xFF, 0xD8, 0xFF, 0xD9],
                    cancellationToken);
            }

            return new ProcessExecutionResult(
                ExitCode: 0,
                StandardOutput: string.Empty,
                StandardError: string.Empty,
                Duration: TimeSpan.FromMilliseconds(100),
                TimedOut: false,
                Cancelled: false);
        }
    }
}