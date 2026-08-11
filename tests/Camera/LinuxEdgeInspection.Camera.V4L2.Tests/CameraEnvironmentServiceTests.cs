using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.V4L2.Models;
using LinuxEdgeInspection.Camera.V4L2.Services;

namespace LinuxEdgeInspection.Camera.V4L2.Tests;

public sealed class CameraEnvironmentServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _devicePath;

    public CameraEnvironmentServiceTests()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "KakipEdgePlatform.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_testDirectory);

        _devicePath = Path.Combine(
            _testDirectory,
            "video0");

        File.WriteAllBytes(
            _devicePath,
            [0x01, 0x02, 0x03]);
    }

    [Fact]
    public async Task CheckAsync_WhenDeviceAndCommandAreAvailable_ReturnsReady()
    {
        var processRunner = new FakeCameraProcessRunner(
            new ProcessExecutionResult(
                ExitCode: 0,
                StandardOutput: "v4l2-ctl 1.26.1",
                StandardError: string.Empty,
                Duration: TimeSpan.FromMilliseconds(10),
                TimedOut: false,
                Cancelled: false));

        var deviceFileSystem = CreateReadyDeviceFileSystem();

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var options = CreateOptions();

        var result = await service.CheckAsync(options);

        Assert.True(result.DeviceExists);
        Assert.True(result.DeviceReadable);
        Assert.True(result.DeviceWritable);
        Assert.True(result.CommandAvailable);
        Assert.True(result.IsReady);

        Assert.Equal(_devicePath, result.DevicePath);
        Assert.Equal("v4l2-ctl", result.CommandPath);
        Assert.Equal(
            "カメラを利用できる環境です。",
            result.Message);
    }

    [Fact]
    public async Task CheckAsync_WhenDeviceDoesNotExist_ReturnsNotReady()
    {
        var processRunner = new FakeCameraProcessRunner(
            CreateSuccessfulProcessResult());

        var deviceFileSystem =
            new FakeCameraDeviceFileSystem(
                new CameraDeviceAccessStatus(
                    Exists: false,
                    Readable: false,
                    Writable: false));

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var options = CreateOptions();

        options.DevicePath = Path.Combine(
            _testDirectory,
            "not-found-video0");

        var result = await service.CheckAsync(options);

        Assert.False(result.DeviceExists);
        Assert.False(result.DeviceReadable);
        Assert.False(result.DeviceWritable);
        Assert.True(result.CommandAvailable);
        Assert.False(result.IsReady);

        Assert.Equal(
            "カメラデバイスが見つかりません。",
            result.Message);
    }

    [Fact]
    public async Task CheckAsync_WhenDeviceIsNotReadable_ReturnsPermissionError()
    {
        var processRunner = new FakeCameraProcessRunner(
            CreateSuccessfulProcessResult());

        var deviceFileSystem =
            new FakeCameraDeviceFileSystem(
                new CameraDeviceAccessStatus(
                    Exists: true,
                    Readable: false,
                    Writable: true));

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var result = await service.CheckAsync(
            CreateOptions());

        Assert.True(result.DeviceExists);
        Assert.False(result.DeviceReadable);
        Assert.True(result.DeviceWritable);
        Assert.False(result.IsReady);

        Assert.Equal(
            "カメラデバイスへアクセスする権限がありません。",
            result.Message);
    }

    [Fact]
    public async Task CheckAsync_WhenDeviceIsNotWritable_ReturnsPermissionError()
    {
        var processRunner = new FakeCameraProcessRunner(
            CreateSuccessfulProcessResult());

        var deviceFileSystem =
            new FakeCameraDeviceFileSystem(
                new CameraDeviceAccessStatus(
                    Exists: true,
                    Readable: true,
                    Writable: false));

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var result = await service.CheckAsync(
            CreateOptions());

        Assert.True(result.DeviceExists);
        Assert.True(result.DeviceReadable);
        Assert.False(result.DeviceWritable);
        Assert.False(result.IsReady);

        Assert.Equal(
            "カメラデバイスへアクセスする権限がありません。",
            result.Message);
    }

    [Fact]
    public async Task CheckAsync_WhenCommandIsUnavailable_ReturnsNotReady()
    {
        var processRunner = new FakeCameraProcessRunner(
            new ProcessExecutionResult(
                ExitCode: null,
                StandardOutput: string.Empty,
                StandardError: "Command not found",
                Duration: TimeSpan.FromMilliseconds(10),
                TimedOut: false,
                Cancelled: false));

        var deviceFileSystem = CreateReadyDeviceFileSystem();

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var options = CreateOptions();

        var result = await service.CheckAsync(options);

        Assert.True(result.DeviceExists);
        Assert.True(result.DeviceReadable);
        Assert.True(result.DeviceWritable);
        Assert.False(result.CommandAvailable);
        Assert.False(result.IsReady);

        Assert.Equal(
            "v4l2-ctlコマンドを利用できません。",
            result.Message);
    }

    [Fact]
    public async Task CheckAsync_WhenCommandTimesOut_ReturnsNotReady()
    {
        var processRunner = new FakeCameraProcessRunner(
            new ProcessExecutionResult(
                ExitCode: null,
                StandardOutput: string.Empty,
                StandardError: string.Empty,
                Duration: TimeSpan.FromSeconds(3),
                TimedOut: true,
                Cancelled: false));

        var deviceFileSystem = CreateReadyDeviceFileSystem();

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var result = await service.CheckAsync(
            CreateOptions());

        Assert.False(result.CommandAvailable);
        Assert.False(result.IsReady);
    }

    [Fact]
    public async Task CheckAsync_WhenCommandPathIsEmpty_DoesNotExecuteProcess()
    {
        var processRunner = new FakeCameraProcessRunner(
            CreateSuccessfulProcessResult());

        var deviceFileSystem = CreateReadyDeviceFileSystem();

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var options = CreateOptions();
        options.V4L2CommandPath = string.Empty;

        var result = await service.CheckAsync(options);

        Assert.False(result.CommandAvailable);
        Assert.False(result.IsReady);
        Assert.Equal(0, processRunner.ExecutionCount);
    }

    [Fact]
    public async Task CheckAsync_ExecutesVersionCommand()
    {
        var processRunner = new FakeCameraProcessRunner(
            CreateSuccessfulProcessResult());

        var deviceFileSystem = CreateReadyDeviceFileSystem();

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        await service.CheckAsync(CreateOptions());

        Assert.Equal(1, processRunner.ExecutionCount);
        Assert.NotNull(processRunner.LastRequest);

        Assert.Equal(
            "v4l2-ctl",
            processRunner.LastRequest.FileName);

        Assert.Equal(
            ["--version"],
            processRunner.LastRequest.Arguments);

        Assert.Equal(
            TimeSpan.FromSeconds(3),
            processRunner.LastRequest.Timeout);
    }

    [Fact]
    public async Task CheckAsync_PassesDevicePathToDeviceFileSystem()
    {
        var processRunner = new FakeCameraProcessRunner(
            CreateSuccessfulProcessResult());

        var deviceFileSystem = CreateReadyDeviceFileSystem();

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var options = CreateOptions();

        await service.CheckAsync(options);

        Assert.Equal(1, deviceFileSystem.CheckCount);
        Assert.Equal(
            options.DevicePath,
            deviceFileSystem.LastDevicePath);
    }

    [Fact]
    public async Task CheckAsync_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        var processRunner = new FakeCameraProcessRunner(
            CreateSuccessfulProcessResult());

        var deviceFileSystem = CreateReadyDeviceFileSystem();

        var service = new CameraEnvironmentService(
            processRunner,
            deviceFileSystem);

        var exception =
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.CheckAsync(null!));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenProcessRunnerIsNull_ThrowsArgumentNullException()
    {
        var deviceFileSystem = CreateReadyDeviceFileSystem();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new CameraEnvironmentService(
                null!,
                deviceFileSystem));

        Assert.Equal("processRunner", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDeviceFileSystemIsNull_ThrowsArgumentNullException()
    {
        var processRunner = new FakeCameraProcessRunner(
            CreateSuccessfulProcessResult());

        var exception = Assert.Throws<ArgumentNullException>(
            () => new CameraEnvironmentService(
                processRunner,
                null!));

        Assert.Equal("deviceFileSystem", exception.ParamName);
    }

    private CameraOptions CreateOptions()
    {
        return new CameraOptions
        {
            DevicePath = _devicePath,
            V4L2CommandPath = "v4l2-ctl"
        };
    }

    private static FakeCameraDeviceFileSystem
        CreateReadyDeviceFileSystem()
    {
        return new FakeCameraDeviceFileSystem(
            new CameraDeviceAccessStatus(
                Exists: true,
                Readable: true,
                Writable: true));
    }

    private static ProcessExecutionResult CreateSuccessfulProcessResult()
    {
        return new ProcessExecutionResult(
            ExitCode: 0,
            StandardOutput: "v4l2-ctl test version",
            StandardError: string.Empty,
            Duration: TimeSpan.FromMilliseconds(10),
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

    private sealed class FakeCameraProcessRunner
        : ICameraProcessRunner
    {
        private readonly ProcessExecutionResult _result;

        public FakeCameraProcessRunner(
            ProcessExecutionResult result)
        {
            _result = result;
        }

        public int ExecutionCount { get; private set; }

        public ProcessExecutionRequest? LastRequest { get; private set; }

        public Task<ProcessExecutionResult> ExecuteAsync(
            ProcessExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ExecutionCount++;
            LastRequest = request;

            return Task.FromResult(_result);
        }
    }

    private sealed class FakeCameraDeviceFileSystem
        : ICameraDeviceFileSystem
    {
        private readonly CameraDeviceAccessStatus _status;

        public FakeCameraDeviceFileSystem(
            CameraDeviceAccessStatus status)
        {
            _status = status;
        }

        public int CheckCount { get; private set; }

        public string? LastDevicePath { get; private set; }

        public CameraDeviceAccessStatus CheckAccess(
            string devicePath)
        {
            CheckCount++;
            LastDevicePath = devicePath;

            return _status;
        }
    }
}