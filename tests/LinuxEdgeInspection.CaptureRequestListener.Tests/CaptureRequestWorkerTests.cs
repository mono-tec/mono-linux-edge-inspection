using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.CaptureRequestListener.Workers;
using Microsoft.Extensions.Logging.Abstractions;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

/// <summary>
/// <see cref="CaptureRequestWorker"/> の動作を確認します。
/// </summary>
public sealed class CaptureRequestWorkerTests
{
    /// <summary>
    /// Queueへ登録したCapture Requestが、
    /// FIFO順に処理されることを確認します。
    /// </summary>
    [Fact]
    public async Task Worker_ProcessesRequestsInFifoOrder()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var requestProcessor =
            new RecordingCaptureRequestProcessor(
                expectedProcessCount: 3);

        var worker =
            new CaptureRequestWorker(
                requestQueue,
                requestProcessor,
                NullLogger<CaptureRequestWorker>.Instance);

        await worker.StartAsync(
            CancellationToken.None);

        try
        {
            // Act
            await requestQueue.EnqueueAsync(
                CreateRequest(101));

            await requestQueue.EnqueueAsync(
                CreateRequest(102));

            await requestQueue.EnqueueAsync(
                CreateRequest(103));

            await requestProcessor.WaitUntilCompletedAsync(
                TimeSpan.FromSeconds(3));

            // Assert
            Assert.Equal(
                [101L, 102L, 103L],
                requestProcessor.ProcessedRequestIds);
        }
        finally
        {
            await worker.StopAsync(
                CancellationToken.None);

            worker.Dispose();
        }
    }

    /// <summary>
    /// Queueが空の場合、
    /// Workerが次のCapture Requestを待機することを確認します。
    /// </summary>
    [Fact]
    public async Task Worker_WaitsWhenQueueIsEmpty()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var requestProcessor =
            new RecordingCaptureRequestProcessor(
                expectedProcessCount: 1);

        var worker =
            new CaptureRequestWorker(
                requestQueue,
                requestProcessor,
                NullLogger<CaptureRequestWorker>.Instance);

        await worker.StartAsync(
            CancellationToken.None);

        try
        {
            // Queueが空の状態では、
            // Processorが呼び出されないことを確認します。
            await Task.Delay(
                TimeSpan.FromMilliseconds(100));

            Assert.Empty(
                requestProcessor.ProcessedRequestIds);

            // Act
            await requestQueue.EnqueueAsync(
                CreateRequest(101));

            await requestProcessor.WaitUntilCompletedAsync(
                TimeSpan.FromSeconds(3));

            // Assert
            Assert.Equal(
                [101L],
                requestProcessor.ProcessedRequestIds);
        }
        finally
        {
            await worker.StopAsync(
                CancellationToken.None);

            worker.Dispose();
        }
    }

    /// <summary>
    /// 前のCapture Requestの処理が完了するまで、
    /// 次のCapture Requestが処理されないことを確認します。
    /// </summary>
    [Fact]
    public async Task Worker_ProcessesNextRequestAfterPreviousRequestCompletes()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var requestProcessor =
            new BlockingCaptureRequestProcessor();

        var worker =
            new CaptureRequestWorker(
                requestQueue,
                requestProcessor,
                NullLogger<CaptureRequestWorker>.Instance);

        await worker.StartAsync(
            CancellationToken.None);

        try
        {
            // Act
            await requestQueue.EnqueueAsync(
                CreateRequest(101));

            await requestQueue.EnqueueAsync(
                CreateRequest(102));

            // 1件目の処理開始まで待機します。
            await requestProcessor.WaitUntilFirstRequestStartedAsync(
                TimeSpan.FromSeconds(3));

            // 1件目が完了していないため、
            // 2件目はまだ処理されません。
            Assert.Equal(
                [101L],
                requestProcessor.ProcessedRequestIds);

            // 1件目を完了させます。
            requestProcessor.CompleteFirstRequest();

            // 2件目の処理開始まで待機します。
            await requestProcessor.WaitUntilSecondRequestStartedAsync(
                TimeSpan.FromSeconds(3));

            Assert.Equal(
                [101L, 102L],
                requestProcessor.ProcessedRequestIds);

            // Workerを正常終了できるよう、
            // 2件目の処理も完了させます。
            requestProcessor.CompleteSecondRequest();
        }
        finally
        {
            await worker.StopAsync(
                CancellationToken.None);

            worker.Dispose();
        }
    }

    /// <summary>
    /// 1件のCapture Request処理で例外が発生しても、
    /// Workerが停止せず次の要求を処理することを確認します。
    /// </summary>
    [Fact]
    public async Task Worker_WhenProcessingThrows_ContinuesWithNextRequest()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var requestProcessor =
            new ThrowingCaptureRequestProcessor(
                failingRequestId: 101,
                expectedProcessCount: 2);

        var worker =
            new CaptureRequestWorker(
                requestQueue,
                requestProcessor,
                NullLogger<CaptureRequestWorker>.Instance);

        await worker.StartAsync(
            CancellationToken.None);

        try
        {
            // Act
            await requestQueue.EnqueueAsync(
                CreateRequest(101));

            await requestQueue.EnqueueAsync(
                CreateRequest(102));

            await requestProcessor.WaitUntilCompletedAsync(
                TimeSpan.FromSeconds(3));

            // Assert
            // 101の処理で例外が発生しても、
            // 102まで処理されていることを確認します。
            Assert.Equal(
                [101L, 102L],
                requestProcessor.ProcessedRequestIds);
        }
        finally
        {
            await worker.StopAsync(
                CancellationToken.None);

            worker.Dispose();
        }
    }

    /// <summary>
    /// テスト用のCapture Requestを生成します。
    /// </summary>
    /// <param name="requestId">
    /// Capture Requestの識別子です。
    /// </param>
    /// <returns>
    /// 指定したRequestIdを持つCapture Requestです。
    /// </returns>
    private static CaptureRequest CreateRequest(
        long requestId)
    {
        return new CaptureRequest(
            RequestId: requestId,
            RequestedAt: DateTimeOffset.Now);
    }

    /// <summary>
    /// 成功したCapture Resultを生成します。
    /// </summary>
    /// <param name="request">
    /// 処理対象のCapture Requestです。
    /// </param>
    /// <returns>
    /// 成功状態のCapture Resultです。
    /// </returns>
    private static CaptureResult CreateSuccessfulResult(
        CaptureRequest request)
    {
        return new CaptureResult(
            RequestId: request.RequestId,
            Succeeded: true,
            CompletedAt: DateTimeOffset.Now,
            ErrorCode: null,
            ErrorMessage: null);
    }

    /// <summary>
    /// 受信したCapture RequestのRequestIdを記録する
    /// テスト用Processorです。
    /// </summary>
    private sealed class RecordingCaptureRequestProcessor
        : ICaptureRequestProcessor
    {
        private readonly int _expectedProcessCount;

        private readonly TaskCompletionSource
            _completed =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private readonly object _syncRoot = new();

        private readonly List<long>
            _processedRequestIds = [];

        public RecordingCaptureRequestProcessor(
            int expectedProcessCount)
        {
            _expectedProcessCount =
                expectedProcessCount;
        }

        /// <summary>
        /// 処理済みRequestIdの一覧です。
        /// </summary>
        public IReadOnlyList<long>
            ProcessedRequestIds
        {
            get
            {
                lock (_syncRoot)
                {
                    return
                        _processedRequestIds.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public Task<CaptureResult> ProcessAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                _processedRequestIds.Add(
                    request.RequestId);

                if (_processedRequestIds.Count >=
                    _expectedProcessCount)
                {
                    _completed.TrySetResult();
                }
            }

            return Task.FromResult(
                CreateSuccessfulResult(request));
        }

        /// <summary>
        /// 想定件数の処理が完了するまで待機します。
        /// </summary>
        public async Task WaitUntilCompletedAsync(
            TimeSpan timeout)
        {
            await _completed.Task.WaitAsync(
                timeout);
        }
    }

    /// <summary>
    /// Capture Requestの処理完了を任意のタイミングまで
    /// 待機させるテスト用Processorです。
    /// </summary>
    private sealed class BlockingCaptureRequestProcessor
        : ICaptureRequestProcessor
    {
        private readonly TaskCompletionSource
            _firstRequestStarted =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private readonly TaskCompletionSource
            _firstRequestCompletion =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private readonly TaskCompletionSource
            _secondRequestStarted =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private readonly TaskCompletionSource
            _secondRequestCompletion =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private readonly object _syncRoot = new();

        private readonly List<long>
            _processedRequestIds = [];

        /// <summary>
        /// 処理を開始したRequestIdの一覧です。
        /// </summary>
        public IReadOnlyList<long>
            ProcessedRequestIds
        {
            get
            {
                lock (_syncRoot)
                {
                    return
                        _processedRequestIds.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public async Task<CaptureResult> ProcessAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            var processIndex = 0;

            lock (_syncRoot)
            {
                _processedRequestIds.Add(
                    request.RequestId);

                processIndex =
                    _processedRequestIds.Count;
            }

            if (processIndex == 1)
            {
                // 1件目の処理が開始されたことを通知します。
                _firstRequestStarted.TrySetResult();

                // テスト側から完了指示が来るまで待機します。
                await _firstRequestCompletion.Task.WaitAsync(
                    cancellationToken);

                return CreateSuccessfulResult(
                    request);
            }

            if (processIndex == 2)
            {
                // 2件目の処理が開始されたことを通知します。
                _secondRequestStarted.TrySetResult();

                // テスト側から完了指示が来るまで待機します。
                await _secondRequestCompletion.Task.WaitAsync(
                    cancellationToken);
            }

            return CreateSuccessfulResult(
                request);
        }

        /// <summary>
        /// 1件目の処理が開始されるまで待機します。
        /// </summary>
        public async Task WaitUntilFirstRequestStartedAsync(
            TimeSpan timeout)
        {
            await _firstRequestStarted.Task.WaitAsync(
                timeout);
        }

        /// <summary>
        /// 2件目の処理が開始されるまで待機します。
        /// </summary>
        public async Task WaitUntilSecondRequestStartedAsync(
            TimeSpan timeout)
        {
            await _secondRequestStarted.Task.WaitAsync(
                timeout);
        }

        /// <summary>
        /// 1件目の処理を完了させます。
        /// </summary>
        public void CompleteFirstRequest()
        {
            _firstRequestCompletion.TrySetResult();
        }

        /// <summary>
        /// 2件目の処理を完了させます。
        /// </summary>
        public void CompleteSecondRequest()
        {
            _secondRequestCompletion.TrySetResult();
        }
    }

    /// <summary>
    /// 指定したRequestIdの処理時に例外を発生させる
    /// テスト用Processorです。
    /// </summary>
    private sealed class ThrowingCaptureRequestProcessor
        : ICaptureRequestProcessor
    {
        private readonly long _failingRequestId;
        private readonly int _expectedProcessCount;

        private readonly TaskCompletionSource
            _completed =
                new(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        private readonly object _syncRoot = new();

        private readonly List<long>
            _processedRequestIds = [];

        public ThrowingCaptureRequestProcessor(
            long failingRequestId,
            int expectedProcessCount)
        {
            _failingRequestId =
                failingRequestId;

            _expectedProcessCount =
                expectedProcessCount;
        }

        /// <summary>
        /// 処理を試行したRequestIdの一覧です。
        /// </summary>
        public IReadOnlyList<long>
            ProcessedRequestIds
        {
            get
            {
                lock (_syncRoot)
                {
                    return
                        _processedRequestIds.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public Task<CaptureResult> ProcessAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                _processedRequestIds.Add(
                    request.RequestId);

                if (_processedRequestIds.Count >=
                    _expectedProcessCount)
                {
                    _completed.TrySetResult();
                }
            }

            // 指定したRequestIdの場合のみ、
            // Capture Request処理中の例外を模擬します。
            if (request.RequestId ==
                _failingRequestId)
            {
                throw new InvalidOperationException(
                    "Capture Requestの処理に失敗しました。");
            }

            return Task.FromResult(
                CreateSuccessfulResult(request));
        }

        /// <summary>
        /// 想定件数の処理が試行されるまで待機します。
        /// </summary>
        public async Task WaitUntilCompletedAsync(
            TimeSpan timeout)
        {
            await _completed.Task.WaitAsync(
                timeout);
        }
    }
}