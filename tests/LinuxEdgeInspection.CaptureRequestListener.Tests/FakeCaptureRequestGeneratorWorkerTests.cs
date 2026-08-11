using LinuxEdgeInspection.CaptureRequestListener.Options;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.CaptureRequestListener.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

/// <summary>
/// <see cref="FakeCaptureRequestGeneratorWorker"/> の動作を確認します。
/// </summary>
public sealed class FakeCaptureRequestGeneratorWorkerTests
{
    /// <summary>
    /// Workerが有効な場合、
    /// 設定されたRequestIdの順序でCapture Requestが生成されることを確認します。
    /// </summary>
    [Fact]
    public async Task Worker_WhenEnabled_GeneratesRequestsInConfiguredOrder()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var options =
            MicrosoftOptions.Create(
                new FakeCaptureRequestGeneratorOptions
                {
                    Enabled = true,
                    InitialDelaySeconds = 0,
                    IntervalSeconds = 0,
                    StartRequestId = 201,
                    RequestCount = 3
                });

        using var worker =
            new FakeCaptureRequestGeneratorWorker(
                requestQueue,
                options,
                NullLogger<
                    FakeCaptureRequestGeneratorWorker>.Instance);

        // Act
        await worker.StartAsync(
            CancellationToken.None);

        // DequeueAsyncはValueTask<CaptureRequest>を返すため、
        // AsTask()でTaskへ変換してからWaitAsync()で
        // タイムアウト付きの待機を行います。
        var request201 =
            await requestQueue
                .DequeueAsync()
                .AsTask()
                .WaitAsync(
                    TimeSpan.FromSeconds(3));

        var request202 =
            await requestQueue
                .DequeueAsync()
                .AsTask()
                .WaitAsync(
                    TimeSpan.FromSeconds(3));

        var request203 =
            await requestQueue
                .DequeueAsync()
                .AsTask()
                .WaitAsync(
                    TimeSpan.FromSeconds(3));

        // Assert
        Assert.Equal(
            201,
            request201.RequestId);

        Assert.Equal(
            202,
            request202.RequestId);

        Assert.Equal(
            203,
            request203.RequestId);

        await worker.StopAsync(
            CancellationToken.None);
    }

    /// <summary>
    /// Workerが無効な場合、
    /// Capture Requestが生成されないことを確認します。
    /// </summary>
    [Fact]
    public async Task Worker_WhenDisabled_DoesNotGenerateRequest()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var options =
            MicrosoftOptions.Create(
                new FakeCaptureRequestGeneratorOptions
                {
                    Enabled = false,
                    InitialDelaySeconds = 0,
                    IntervalSeconds = 0,
                    StartRequestId = 101,
                    RequestCount = 3
                });

        using var worker =
            new FakeCaptureRequestGeneratorWorker(
                requestQueue,
                options,
                NullLogger<
                    FakeCaptureRequestGeneratorWorker>.Instance);

        await worker.StartAsync(
            CancellationToken.None);

        // QueueにCapture Requestが追加されないことを確認するため、
        // 一定時間後にキャンセルされるCancellationTokenを使用します。
        using var cancellationTokenSource =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(200));

        // Assert
        //
        // DequeueAsyncはValueTaskを返すため、
        // Assert.ThrowsAnyAsyncへ直接渡さず、
        // asyncラムダでTaskへ変換します。
        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            async () =>
                await requestQueue.DequeueAsync(
                    cancellationTokenSource.Token));

        await worker.StopAsync(
            CancellationToken.None);
    }

    /// <summary>
    /// RequestCountが0の場合、
    /// Capture Requestが生成されないことを確認します。
    /// </summary>
    [Fact]
    public async Task Worker_WhenRequestCountIsZero_DoesNotGenerateRequest()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var options =
            MicrosoftOptions.Create(
                new FakeCaptureRequestGeneratorOptions
                {
                    Enabled = true,
                    InitialDelaySeconds = 0,
                    IntervalSeconds = 0,
                    StartRequestId = 101,
                    RequestCount = 0
                });

        using var worker =
            new FakeCaptureRequestGeneratorWorker(
                requestQueue,
                options,
                NullLogger<
                    FakeCaptureRequestGeneratorWorker>.Instance);

        await worker.StartAsync(
            CancellationToken.None);

        // RequestCountが0のため、
        // QueueにはCapture Requestが追加されません。
        using var cancellationTokenSource =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(200));

        // Assert
        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            async () =>
                await requestQueue.DequeueAsync(
                    cancellationTokenSource.Token));

        await worker.StopAsync(
            CancellationToken.None);
    }

    /// <summary>
    /// StartRequestIdとRequestCountの設定値が、
    /// 生成されるCapture Requestへ正しく反映されることを確認します。
    /// </summary>
    [Fact]
    public async Task Worker_UsesConfiguredStartRequestIdAndRequestCount()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var options =
            MicrosoftOptions.Create(
                new FakeCaptureRequestGeneratorOptions
                {
                    Enabled = true,
                    InitialDelaySeconds = 0,
                    IntervalSeconds = 0,
                    StartRequestId = 500,
                    RequestCount = 2
                });

        using var worker =
            new FakeCaptureRequestGeneratorWorker(
                requestQueue,
                options,
                NullLogger<
                    FakeCaptureRequestGeneratorWorker>.Instance);

        // Act
        await worker.StartAsync(
            CancellationToken.None);

        var firstRequest =
            await requestQueue
                .DequeueAsync()
                .AsTask()
                .WaitAsync(
                    TimeSpan.FromSeconds(3));

        var secondRequest =
            await requestQueue
                .DequeueAsync()
                .AsTask()
                .WaitAsync(
                    TimeSpan.FromSeconds(3));

        // Assert
        Assert.Equal(
            500,
            firstRequest.RequestId);

        Assert.Equal(
            501,
            secondRequest.RequestId);

        await worker.StopAsync(
            CancellationToken.None);
    }

    /// <summary>
    /// CaptureRequestQueueがnullの場合、
    /// ArgumentNullExceptionがスローされることを確認します。
    /// </summary>
    [Fact]
    public void Constructor_WhenRequestQueueIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var options =
            MicrosoftOptions.Create(
                new FakeCaptureRequestGeneratorOptions());

        // Act
        var exception =
            Assert.Throws<ArgumentNullException>(
                () =>
                    new FakeCaptureRequestGeneratorWorker(
                        null!,
                        options,
                        NullLogger<
                            FakeCaptureRequestGeneratorWorker>.Instance));

        // Assert
        Assert.Equal(
            "requestQueue",
            exception.ParamName);
    }

    /// <summary>
    /// Optionsがnullの場合、
    /// ArgumentNullExceptionがスローされることを確認します。
    /// </summary>
    [Fact]
    public void Constructor_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        // Act
        var exception =
            Assert.Throws<ArgumentNullException>(
                () =>
                    new FakeCaptureRequestGeneratorWorker(
                        requestQueue,
                        null!,
                        NullLogger<
                            FakeCaptureRequestGeneratorWorker>.Instance));

        // Assert
        Assert.Equal(
            "options",
            exception.ParamName);
    }

    /// <summary>
    /// Loggerがnullの場合、
    /// ArgumentNullExceptionがスローされることを確認します。
    /// </summary>
    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var requestQueue =
            new CaptureRequestQueue();

        var options =
            MicrosoftOptions.Create(
                new FakeCaptureRequestGeneratorOptions());

        // Act
        var exception =
            Assert.Throws<ArgumentNullException>(
                () =>
                    new FakeCaptureRequestGeneratorWorker(
                        requestQueue,
                        options,
                        null!));

        // Assert
        Assert.Equal(
            "logger",
            exception.ParamName);
    }
}