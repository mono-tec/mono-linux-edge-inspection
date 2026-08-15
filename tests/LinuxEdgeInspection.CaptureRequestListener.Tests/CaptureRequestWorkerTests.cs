using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.CaptureRequestListener.Workers;
using LinuxEdgeInspection.Contracts.Capture;
using Microsoft.Extensions.Logging.Abstractions;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

public sealed class CaptureRequestWorkerTests
{
    [Fact]
    public async Task Worker_ProcessesRequestsSequentially()
    {
        var queue = new CaptureRequestQueue();
        var processor = new TrackingProcessor();
        var worker = new CaptureRequestWorker(
            queue,
            processor,
            NullLogger<CaptureRequestWorker>.Instance);
        var first = CreateItem("REQ-001", 1);
        var second = CreateItem("REQ-002", 1);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            await queue.EnqueueAsync(first);
            await processor.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await queue.EnqueueAsync(second);
            processor.ReleaseFirst.TrySetResult();

            await Task.WhenAll(first.Completion, second.Completion)
                .WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(
                ["REQ-001", "REQ-002"],
                processor.RequestIds);
            Assert.Equal(1, processor.MaximumConcurrency);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_WhenProcessorThrows_CompletesWaitingItemWithFailure()
    {
        var queue = new CaptureRequestQueue();
        var worker = new CaptureRequestWorker(
            queue,
            new ThrowingProcessor(),
            NullLogger<CaptureRequestWorker>.Instance);
        var item = CreateItem("REQ-FAIL", 1);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            await queue.EnqueueAsync(item);
            var result = await item.Completion.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.False(result.Succeeded);
            Assert.Equal("CAPTURE_PROCESSING_FAILED", result.ErrorCode);
            Assert.Equal(item.Request.RequestId, result.RequestId);
            Assert.Equal(item.Request.CaptureIndex, result.CaptureIndex);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private static CaptureRequestQueueItem CreateItem(
        string requestId,
        int captureIndex) =>
        new(new CaptureRequest(requestId, captureIndex, DateTimeOffset.Now));

    private sealed class TrackingProcessor : ICaptureRequestProcessor
    {
        private int _concurrency;
        private int _maximumConcurrency;

        public TaskCompletionSource FirstStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirst { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<string> RequestIds { get; } = [];

        public int MaximumConcurrency => _maximumConcurrency;

        public async Task<CaptureResult> ProcessAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            var concurrency = Interlocked.Increment(ref _concurrency);
            InterlockedExtensions.Max(ref _maximumConcurrency, concurrency);
            RequestIds.Add(request.RequestId);

            try
            {
                if (RequestIds.Count == 1)
                {
                    FirstStarted.TrySetResult();
                    await ReleaseFirst.Task.WaitAsync(cancellationToken);
                }

                return new CaptureResult(
                    request.RequestId,
                    request.CaptureIndex,
                    true,
                    DateTimeOffset.Now,
                    null,
                    null);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrency);
            }
        }
    }

    private sealed class ThrowingProcessor : ICaptureRequestProcessor
    {
        public Task<CaptureResult> ProcessAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("test failure");
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int target, int value)
        {
            int current;
            do
            {
                current = Volatile.Read(ref target);
                if (current >= value)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref target, value, current) != current);
        }
    }
}
