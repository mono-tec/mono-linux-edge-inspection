using LinuxEdgeInspection.Camera.V4L2.Models;
using LinuxEdgeInspection.Camera.V4L2.Services;

namespace LinuxEdgeInspection.Camera.V4L2.Tests;

public sealed class CameraProcessRunnerTests
{
    private readonly CameraProcessRunner _runner = new();

    [Fact]
    public async Task ExecuteAsync_WhenProcessSucceeds_ReturnsSuccessfulResult()
    {
        var request = CreateSuccessRequest();

        var result = await _runner.ExecuteAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.False(result.TimedOut);
        Assert.False(result.Cancelled);
        Assert.Contains(
            "camera-test-success",
            result.StandardOutput);
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessReturnsNonZeroExitCode_ReturnsFailedResult()
    {
        var request = CreateFailureRequest();

        var result = await _runner.ExecuteAsync(request);

        Assert.False(result.Succeeded);
        Assert.NotEqual(0, result.ExitCode);
        Assert.False(result.TimedOut);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessWritesStandardError_CapturesStandardError()
    {
        var request = CreateStandardErrorRequest();

        var result = await _runner.ExecuteAsync(request);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "camera-test-error",
            result.StandardError);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimeoutOccurs_ReturnsTimedOutResult()
    {
        var request = CreateDelayRequest(
            processDuration: TimeSpan.FromSeconds(5),
            timeout: TimeSpan.FromMilliseconds(200));

        var result = await _runner.ExecuteAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(result.ExitCode);
        Assert.True(result.TimedOut);
        Assert.False(result.Cancelled);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationIsRequested_ReturnsCancelledResult()
    {
        var request = CreateDelayRequest(
            processDuration: TimeSpan.FromSeconds(5),
            timeout: TimeSpan.FromSeconds(10));

        using var cancellationTokenSource =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(200));

        var result = await _runner.ExecuteAsync(
            request,
            cancellationTokenSource.Token);

        Assert.False(result.Succeeded);
        Assert.Null(result.ExitCode);
        Assert.False(result.TimedOut);
        Assert.True(result.Cancelled);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandDoesNotExist_ReturnsFailedResult()
    {
        var request = new ProcessExecutionRequest(
            FileName: "command-that-does-not-exist-kakip-test",
            Arguments: [],
            Timeout: TimeSpan.FromSeconds(1));

        var result = await _runner.ExecuteAsync(request);

        Assert.False(result.Succeeded);
        Assert.Null(result.ExitCode);
        Assert.False(result.TimedOut);
        Assert.False(result.Cancelled);
        Assert.False(
            string.IsNullOrWhiteSpace(result.StandardError));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _runner.ExecuteAsync(null!));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFileNameIsEmpty_ThrowsArgumentException()
    {
        var request = new ProcessExecutionRequest(
            FileName: string.Empty,
            Arguments: [],
            Timeout: TimeSpan.FromSeconds(1));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _runner.ExecuteAsync(request));

        Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimeoutIsZero_ThrowsArgumentOutOfRangeException()
    {
        var request = new ProcessExecutionRequest(
            FileName: GetShellFileName(),
            Arguments: [],
            Timeout: TimeSpan.Zero);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _runner.ExecuteAsync(request));

        Assert.Equal("request", exception.ParamName);
    }

    private static ProcessExecutionRequest CreateSuccessRequest()
    {
        if (OperatingSystem.IsWindows())
        {
            return new ProcessExecutionRequest(
                FileName: "cmd.exe",
                Arguments:
                [
                    "/c",
                    "echo camera-test-success"
                ],
                Timeout: TimeSpan.FromSeconds(5));
        }

        return new ProcessExecutionRequest(
            FileName: "/bin/sh",
            Arguments:
            [
                "-c",
                "echo camera-test-success"
            ],
            Timeout: TimeSpan.FromSeconds(5));
    }

    private static ProcessExecutionRequest CreateFailureRequest()
    {
        if (OperatingSystem.IsWindows())
        {
            return new ProcessExecutionRequest(
                FileName: "cmd.exe",
                Arguments:
                [
                    "/c",
                    "exit /b 5"
                ],
                Timeout: TimeSpan.FromSeconds(5));
        }

        return new ProcessExecutionRequest(
            FileName: "/bin/sh",
            Arguments:
            [
                "-c",
                "exit 5"
            ],
            Timeout: TimeSpan.FromSeconds(5));
    }

    private static ProcessExecutionRequest CreateStandardErrorRequest()
    {
        if (OperatingSystem.IsWindows())
        {
            return new ProcessExecutionRequest(
                FileName: "cmd.exe",
                Arguments:
                [
                    "/c",
                    "echo camera-test-error 1>&2 & exit /b 1"
                ],
                Timeout: TimeSpan.FromSeconds(5));
        }

        return new ProcessExecutionRequest(
            FileName: "/bin/sh",
            Arguments:
            [
                "-c",
                "echo camera-test-error 1>&2; exit 1"
            ],
            Timeout: TimeSpan.FromSeconds(5));
    }

    private static ProcessExecutionRequest CreateDelayRequest(
        TimeSpan processDuration,
        TimeSpan timeout)
    {
        var delaySeconds = Math.Max(
            1,
            (int)Math.Ceiling(processDuration.TotalSeconds));

        if (OperatingSystem.IsWindows())
        {
            return new ProcessExecutionRequest(
                FileName: "powershell.exe",
                Arguments:
                [
                    "-NoProfile",
                    "-NonInteractive",
                    "-Command",
                    $"Start-Sleep -Seconds {delaySeconds}"
                ],
                Timeout: timeout);
        }

        return new ProcessExecutionRequest(
            FileName: "/bin/sh",
            Arguments:
            [
                "-c",
                $"sleep {delaySeconds}"
            ],
            Timeout: timeout);
    }

    private static string GetShellFileName()
    {
        return OperatingSystem.IsWindows()
            ? "cmd.exe"
            : "/bin/sh";
    }
}