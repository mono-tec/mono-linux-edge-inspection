using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.Contracts.Capture;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

public sealed class CaptureRequestQueueTests
{
    [Fact]
    public async Task EnqueueAndDequeueAsync_PreservesFifoOrder()
    {
        var queue = new CaptureRequestQueue();
        var items = Enumerable.Range(1, 3)
            .Select(index => new CaptureRequestQueueItem(
                CreateRequest($"REQ-{index}", index)))
            .ToArray();

        foreach (var item in items)
        {
            await queue.EnqueueAsync(item);
        }

        foreach (var expected in items)
        {
            Assert.Same(expected, await queue.DequeueAsync());
        }
    }

    [Fact]
    public async Task QueueItem_CompletionReturnsMatchingResult()
    {
        var request = CreateRequest("REQ-001", 1);
        var item = new CaptureRequestQueueItem(request);
        var result = new CaptureResult(
            RequestId: request.RequestId,
            CaptureIndex: request.CaptureIndex,
            Succeeded: true,
            CompletedAt: DateTimeOffset.Now,
            FilePath: null,
            ErrorCode: null,
            ErrorMessage: null);

        Assert.True(item.TrySetResult(result));

        Assert.Same(result, await item.Completion);
    }

    [Fact]
    public async Task EnqueueAsync_WhenItemIsNull_Throws()
    {
        var queue = new CaptureRequestQueue();
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await queue.EnqueueAsync(null!));
    }

    [Fact]
    public async Task DequeueAsync_WhenCancelled_Throws()
    {
        var queue = new CaptureRequestQueue();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await queue.DequeueAsync(source.Token));
    }

    private static CaptureRequest CreateRequest(
        string requestId,
        int captureIndex) =>
        new(requestId, captureIndex, DateTimeOffset.Now);
}
