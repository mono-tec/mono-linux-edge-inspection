using LinuxEdgeInspection.CaptureRequestListener.Options;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.CaptureRequestListener.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

public sealed class FakeCaptureRequestGeneratorWorkerTests
{
    [Fact]
    public async Task Enabled_GeneratesRequestsThroughInterfaceQueue()
    {
        ICaptureRequestQueue queue = new CaptureRequestQueue();
        var worker = new FakeCaptureRequestGeneratorWorker(
            queue,
            Microsoft.Extensions.Options.Options.Create(
                new FakeCaptureRequestGeneratorOptions
            {
                Enabled = true,
                InitialDelaySeconds = 0,
                IntervalSeconds = 0,
                StartRequestId = 101,
                RequestCount = 2
                }),
            NullLogger<FakeCaptureRequestGeneratorWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            var first = await queue.DequeueAsync()
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            var second = await queue.DequeueAsync()
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal("101", first.Request.RequestId);
            Assert.Equal("102", second.Request.RequestId);
            Assert.Equal(1, first.Request.CaptureIndex);
            Assert.Equal(1, second.Request.CaptureIndex);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Disabled_DoesNotGenerateRequest()
    {
        ICaptureRequestQueue queue = new CaptureRequestQueue();
        var worker = new FakeCaptureRequestGeneratorWorker(
            queue,
            Microsoft.Extensions.Options.Options.Create(
                new FakeCaptureRequestGeneratorOptions
            {
                Enabled = false
                }),
            NullLogger<FakeCaptureRequestGeneratorWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await queue.DequeueAsync(source.Token));
    }
}
