using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

public sealed class SystemdCaptureRuntimeLauncherTests
{
    [Fact]
    public async Task LaunchAsync_WhenCommandSucceeds_ReturnsSuccessfulResult()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                new SystemCommandExecutionResult(
                    ExitCode: 0,
                    StandardOutput: string.Empty,
                    StandardError: string.Empty,
                    Duration: TimeSpan.FromMilliseconds(100),
                    TimedOut: false,
                    Cancelled: false));

        var launcher =
            CreateLauncher(commandRunner);

        var result =
            await launcher.LaunchAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);

        Assert.True(
            result.CompletedAt >= result.StartedAt);
    }

    [Fact]
    public async Task LaunchAsync_ExecutesSystemctlRestartCommand()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                CreateSuccessfulExecutionResult());

        var launcher =
            CreateLauncher(commandRunner);

        await launcher.LaunchAsync();

        Assert.Equal(
            1,
            commandRunner.ExecutionCount);

        Assert.Equal(
            "/usr/bin/systemctl",
            commandRunner.LastFileName);

        Assert.Equal(
            [
                "restart",
                "kakip-edge-platform-runtime.service"
            ],
            commandRunner.LastArguments);

        Assert.Equal(
            TimeSpan.FromSeconds(30),
            commandRunner.LastTimeout);
    }

    [Fact]
    public async Task LaunchAsync_WhenCommandFails_ReturnsLaunchFailedResult()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                new SystemCommandExecutionResult(
                    ExitCode: 1,
                    StandardOutput: string.Empty,
                    StandardError:
                        "Failed to restart service.",
                    Duration: TimeSpan.FromMilliseconds(100),
                    TimedOut: false,
                    Cancelled: false));

        var launcher =
            CreateLauncher(commandRunner);

        var result =
            await launcher.LaunchAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "PLC-E001",
            result.ErrorCode);

        Assert.Equal(
            "Failed to restart service.",
            result.ErrorMessage);
    }

    [Fact]
    public async Task LaunchAsync_WhenStandardErrorIsEmpty_ReturnsDefaultErrorMessage()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                new SystemCommandExecutionResult(
                    ExitCode: 1,
                    StandardOutput: string.Empty,
                    StandardError: string.Empty,
                    Duration: TimeSpan.FromMilliseconds(100),
                    TimedOut: false,
                    Cancelled: false));

        var launcher =
            CreateLauncher(commandRunner);

        var result =
            await launcher.LaunchAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(
            "PLC-E001",
            result.ErrorCode);

        Assert.Equal(
            "撮影Runtimeの起動に失敗しました。",
            result.ErrorMessage);
    }

    [Fact]
    public async Task LaunchAsync_WhenCommandTimesOut_ReturnsTimeoutResult()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                new SystemCommandExecutionResult(
                    ExitCode: null,
                    StandardOutput: string.Empty,
                    StandardError: string.Empty,
                    Duration: TimeSpan.FromSeconds(30),
                    TimedOut: true,
                    Cancelled: false));

        var launcher =
            CreateLauncher(commandRunner);

        var result =
            await launcher.LaunchAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.ExitCode);
        Assert.Equal(
            "PLC-E002",
            result.ErrorCode);

        Assert.Equal(
            "撮影Runtimeの起動処理がタイムアウトしました。",
            result.ErrorMessage);
    }

    [Fact]
    public async Task LaunchAsync_WhenCommandIsCancelled_ReturnsCancelledResult()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                new SystemCommandExecutionResult(
                    ExitCode: null,
                    StandardOutput: string.Empty,
                    StandardError: string.Empty,
                    Duration: TimeSpan.FromMilliseconds(100),
                    TimedOut: false,
                    Cancelled: true));

        var launcher =
            CreateLauncher(commandRunner);

        var result =
            await launcher.LaunchAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.ExitCode);
        Assert.Equal(
            "PLC-E003",
            result.ErrorCode);

        Assert.Equal(
            "撮影Runtimeの起動処理がキャンセルされました。",
            result.ErrorMessage);
    }

    [Fact]
    public async Task LaunchAsync_PassesCancellationToken()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                CreateSuccessfulExecutionResult());

        var launcher =
            CreateLauncher(commandRunner);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        await launcher.LaunchAsync(
            cancellationTokenSource.Token);

        Assert.Equal(
            cancellationTokenSource.Token,
            commandRunner.LastCancellationToken);
    }

    [Fact]
    public void Constructor_WhenCommandRunnerIsNull_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new SystemdCaptureRuntimeLauncher(
                    null!,
                    "/usr/bin/systemctl",
                    "kakip-edge-platform-runtime.service",
                    TimeSpan.FromSeconds(30)));

        Assert.Equal(
            "commandRunner",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenSystemctlPathIsEmpty_ThrowsArgumentException()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                CreateSuccessfulExecutionResult());

        var exception =
            Assert.Throws<ArgumentException>(
                () => new SystemdCaptureRuntimeLauncher(
                    commandRunner,
                    string.Empty,
                    "kakip-edge-platform-runtime.service",
                    TimeSpan.FromSeconds(30)));

        Assert.Equal(
            "systemctlPath",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenServiceNameIsEmpty_ThrowsArgumentException()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                CreateSuccessfulExecutionResult());

        var exception =
            Assert.Throws<ArgumentException>(
                () => new SystemdCaptureRuntimeLauncher(
                    commandRunner,
                    "/usr/bin/systemctl",
                    string.Empty,
                    TimeSpan.FromSeconds(30)));

        Assert.Equal(
            "serviceName",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenTimeoutIsZero_ThrowsArgumentOutOfRangeException()
    {
        var commandRunner =
            new FakeSystemCommandRunner(
                CreateSuccessfulExecutionResult());

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SystemdCaptureRuntimeLauncher(
                    commandRunner,
                    "/usr/bin/systemctl",
                    "kakip-edge-platform-runtime.service",
                    TimeSpan.Zero));

        Assert.Equal(
            "timeout",
            exception.ParamName);
    }

    private static SystemdCaptureRuntimeLauncher CreateLauncher(
        ISystemCommandRunner commandRunner)
    {
        return new SystemdCaptureRuntimeLauncher(
            commandRunner,
            systemctlPath:
                "/usr/bin/systemctl",
            serviceName:
                "kakip-edge-platform-runtime.service",
            timeout:
                TimeSpan.FromSeconds(30));
    }

    private static SystemCommandExecutionResult
        CreateSuccessfulExecutionResult()
    {
        return new SystemCommandExecutionResult(
            ExitCode: 0,
            StandardOutput: string.Empty,
            StandardError: string.Empty,
            Duration: TimeSpan.FromMilliseconds(100),
            TimedOut: false,
            Cancelled: false);
    }

    private sealed class FakeSystemCommandRunner
        : ISystemCommandRunner
    {
        private readonly SystemCommandExecutionResult
            _result;

        public FakeSystemCommandRunner(
            SystemCommandExecutionResult result)
        {
            _result = result;
        }

        public int ExecutionCount { get; private set; }

        public string? LastFileName { get; private set; }

        public IReadOnlyList<string>?
            LastArguments
        { get; private set; }

        public TimeSpan LastTimeout { get; private set; }

        public CancellationToken
            LastCancellationToken
        { get; private set; }

        public Task<SystemCommandExecutionResult> ExecuteAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;

            LastFileName = fileName;
            LastArguments = arguments.ToArray();
            LastTimeout = timeout;
            LastCancellationToken =
                cancellationToken;

            return Task.FromResult(
                _result);
        }
    }
}