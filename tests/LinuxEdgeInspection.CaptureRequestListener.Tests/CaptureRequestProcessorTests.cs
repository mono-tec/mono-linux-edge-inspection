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
        var completedAt = DateTimeOffset.Now;
        var launcher = new FakeLauncher(new CaptureRuntimeLaunchResult(
            succeeded,
            succeeded ? 0 : 1,
            completedAt.AddSeconds(-1),
            completedAt,
            errorCode,
            errorCode is null ? null : "failed"));
        var processor = new CaptureRequestProcessor(launcher);
        var request = new CaptureRequest("REQ-001", 1, DateTimeOffset.Now);

        var result = await processor.ProcessAsync(request);

        Assert.Equal(request.RequestId, result.RequestId);
        Assert.Equal(request.CaptureIndex, result.CaptureIndex);
        Assert.Equal(succeeded, result.Succeeded);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Equal(completedAt, result.CompletedAt);
    }

    [Fact]
    public async Task ProcessAsync_PassesCancellationToken()
    {
        var launcher = new FakeLauncher(new CaptureRuntimeLaunchResult(
            true, 0, DateTimeOffset.Now, DateTimeOffset.Now, null, null));
        var processor = new CaptureRequestProcessor(launcher);
        using var source = new CancellationTokenSource();

        await processor.ProcessAsync(
            new CaptureRequest("REQ-001", 1, DateTimeOffset.Now),
            source.Token);

        Assert.Equal(source.Token, launcher.LastToken);
    }

    [Fact]
    public async Task ProcessAsync_WhenRequestIsNull_Throws()
    {
        var launcher = new FakeLauncher(new CaptureRuntimeLaunchResult(
            true, 0, DateTimeOffset.Now, DateTimeOffset.Now, null, null));
        var processor = new CaptureRequestProcessor(launcher);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => processor.ProcessAsync(null!));
    }

    private sealed class FakeLauncher : ICaptureRuntimeLauncher
    {
        private readonly CaptureRuntimeLaunchResult _result;

        public FakeLauncher(CaptureRuntimeLaunchResult result) =>
            _result = result;

        public CancellationToken LastToken { get; private set; }

        public Task<CaptureRuntimeLaunchResult> LaunchAsync(
            CancellationToken cancellationToken = default)
        {
            LastToken = cancellationToken;
            return Task.FromResult(_result);
        }
    }
}
