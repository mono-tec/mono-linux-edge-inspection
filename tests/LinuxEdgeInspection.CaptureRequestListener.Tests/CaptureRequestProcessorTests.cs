using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.Contracts.Capture;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

public sealed class CaptureRequestProcessorTests
{
    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "CAPTURE_RUNTIME_LAUNCH_FAILED")]
    public async Task ProcessAsync_MapsRuntimeResult(
        bool succeeded,
        string? errorCode)
    {
        var completedAt =
            DateTimeOffset.Now;

        var launcher =
            new FakeLauncher(
                new CaptureRuntimeLaunchResult(
                    Succeeded: succeeded,
                    ExitCode: succeeded ? 0 : 1,
                    StartedAt: completedAt.AddSeconds(-1),
                    CompletedAt: completedAt,
                    FilePath: null,
                    ErrorCode: errorCode,
                    ErrorMessage:
                        errorCode is null
                            ? null
                            : "failed"));

        var processor =
            new CaptureRequestProcessor(
                launcher);

        var request =
            new CaptureRequest(
                "REQ-001",
                1,
                DateTimeOffset.Now);

        var result =
            await processor.ProcessAsync(
                request);

        Assert.Equal(
            request.RequestId,
            result.RequestId);

        Assert.Equal(
            request.CaptureIndex,
            result.CaptureIndex);

        Assert.Equal(
            succeeded,
            result.Succeeded);

        Assert.Null(
            result.FilePath);

        Assert.Equal(
            errorCode,
            result.ErrorCode);

        Assert.Equal(
            completedAt,
            result.CompletedAt);
    }

    [Fact]
    public async Task ProcessAsync_PassesCancellationToken()
    {
        var launcher =
            new FakeLauncher(
                new CaptureRuntimeLaunchResult(
                    Succeeded: true,
                    ExitCode: 0,
                    StartedAt: DateTimeOffset.Now,
                    CompletedAt: DateTimeOffset.Now,
                    FilePath: null,
                    ErrorCode: null,
                    ErrorMessage: null));

        var processor =
            new CaptureRequestProcessor(
                launcher);

        using var source =
            new CancellationTokenSource();

        await processor.ProcessAsync(
            new CaptureRequest(
                "REQ-001",
                1,
                DateTimeOffset.Now),
            source.Token);

        Assert.Equal(
            source.Token,
            launcher.LastToken);
    }

    [Fact]
    public async Task ProcessAsync_WhenRequestIsNull_Throws()
    {
        var launcher =
            new FakeLauncher(
                new CaptureRuntimeLaunchResult(
                    Succeeded: true,
                    ExitCode: 0,
                    StartedAt: DateTimeOffset.Now,
                    CompletedAt: DateTimeOffset.Now,
                    FilePath: null,
                    ErrorCode: null,
                    ErrorMessage: null));

        var processor =
            new CaptureRequestProcessor(
                launcher);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => processor.ProcessAsync(
                null!));
    }

    [Fact]
    public async Task ProcessAsync_WhenRuntimeLaunchSucceeds_ReturnsFilePath()
    {
        var request =
            new CaptureRequest(
                RequestId: "REQ-001",
                CaptureIndex: 1,
                RequestedAt: DateTimeOffset.UtcNow);

        var expectedFilePath =
            "/var/lib/linux-edge-inspection-runtime/captures/capture.jpg";

        var launcher =
            new FakeLauncher(
                new CaptureRuntimeLaunchResult(
                    Succeeded: true,
                    ExitCode: 0,
                    StartedAt: DateTimeOffset.UtcNow,
                    CompletedAt: DateTimeOffset.UtcNow,
                    FilePath: expectedFilePath,
                    ErrorCode: null,
                    ErrorMessage: null));

        var processor =
            new CaptureRequestProcessor(
                launcher);

        var result =
            await processor.ProcessAsync(
                request);

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            request.RequestId,
            result.RequestId);

        Assert.Equal(
            request.CaptureIndex,
            result.CaptureIndex);

        Assert.Equal(
            expectedFilePath,
            result.FilePath);

        Assert.Null(
            result.ErrorCode);

        Assert.Null(
            result.ErrorMessage);
    }

    private sealed class FakeLauncher
        : ICaptureRuntimeLauncher
    {
        private readonly CaptureRuntimeLaunchResult _result;

        public FakeLauncher(
            CaptureRuntimeLaunchResult result)
        {
            _result = result;
        }

        public CancellationToken LastToken
        {
            get;
            private set;
        }

        public Task<CaptureRuntimeLaunchResult> LaunchAsync(
            CancellationToken cancellationToken = default)
        {
            LastToken =
                cancellationToken;

            return Task.FromResult(
                _result);
        }
    }
}