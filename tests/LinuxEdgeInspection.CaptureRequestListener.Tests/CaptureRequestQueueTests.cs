using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

public sealed class CaptureRequestQueueTests
{
    [Fact]
    public async Task EnqueueAndDequeueAsync_ReturnsRequestsInFifoOrder()
    {
        var queue = new CaptureRequestQueue();

        var request101 = new CaptureRequest(
            RequestId: 101,
            RequestedAt: DateTimeOffset.Now);

        var request102 = new CaptureRequest(
            RequestId: 102,
            RequestedAt: DateTimeOffset.Now.AddMilliseconds(1));

        var request103 = new CaptureRequest(
            RequestId: 103,
            RequestedAt: DateTimeOffset.Now.AddMilliseconds(2));

        await queue.EnqueueAsync(request101);
        await queue.EnqueueAsync(request102);
        await queue.EnqueueAsync(request103);

        var actual101 = await queue.DequeueAsync();
        var actual102 = await queue.DequeueAsync();
        var actual103 = await queue.DequeueAsync();

        Assert.Equal(
            101,
            actual101.RequestId);

        Assert.Equal(
            102,
            actual102.RequestId);

        Assert.Equal(
            103,
            actual103.RequestId);
    }

    [Fact]
    public async Task EnqueueAndDequeueAsync_ReturnsSameRequest()
    {
        var queue = new CaptureRequestQueue();

        var expected = new CaptureRequest(
            RequestId: 101,
            RequestedAt: DateTimeOffset.Now);

        await queue.EnqueueAsync(expected);

        var actual =
            await queue.DequeueAsync();

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public async Task DequeueAsync_WhenQueueIsEmpty_WaitsUntilRequestIsEnqueued()
    {
        var queue = new CaptureRequestQueue();

        var dequeueTask =
            queue.DequeueAsync().AsTask();

        Assert.False(
            dequeueTask.IsCompleted);

        var expected = new CaptureRequest(
            RequestId: 101,
            RequestedAt: DateTimeOffset.Now);

        await queue.EnqueueAsync(expected);

        var actual =
            await dequeueTask;

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public async Task DequeueAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var queue = new CaptureRequestQueue();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var dequeueTask =
            queue.DequeueAsync(
                cancellationTokenSource.Token)
            .AsTask();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dequeueTask);
    }

    [Fact]
    public async Task EnqueueAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var queue = new CaptureRequestQueue();

        var exception =
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                    await queue.EnqueueAsync(null!));

        Assert.Equal(
            "request",
            exception.ParamName);
    }

    [Fact]
    public async Task MultipleWriters_RequestsCanBeAddedSafely()
    {
        var queue = new CaptureRequestQueue();

        var enqueueTasks =
            Enumerable.Range(1, 100)
                .Select(
                    requestId =>
                        queue.EnqueueAsync(
                                new CaptureRequest(
                                    RequestId: requestId,
                                    RequestedAt: DateTimeOffset.Now))
                            .AsTask())
                .ToArray();

        await Task.WhenAll(
            enqueueTasks);

        var actualRequestIds =
            new List<long>();

        for (var index = 0; index < 100; index++)
        {
            var request =
                await queue.DequeueAsync();

            actualRequestIds.Add(
                request.RequestId);
        }

        Assert.Equal(
            100,
            actualRequestIds.Count);

        Assert.Equal(
            Enumerable.Range(1, 100)
                .Select(
                    requestId =>
                        (long)requestId)
                .OrderBy(
                    requestId =>
                        requestId),
            actualRequestIds.OrderBy(
                requestId =>
                    requestId));
    }
}