using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.Contracts.Capture;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinuxEdgeInspection.CaptureRequestListener.Workers;

/// <summary>
/// Capture RequestをFIFO順に1件ずつ処理します。
/// </summary>
public sealed class CaptureRequestWorker : BackgroundService
{
    private readonly ICaptureRequestQueue _requestQueue;
    private readonly ICaptureRequestProcessor _requestProcessor;
    private readonly ILogger<CaptureRequestWorker> _logger;

    public CaptureRequestWorker(
        ICaptureRequestQueue requestQueue,
        ICaptureRequestProcessor requestProcessor,
        ILogger<CaptureRequestWorker> logger)
    {
        _requestQueue = requestQueue
            ?? throw new ArgumentNullException(nameof(requestQueue));
        _requestProcessor = requestProcessor
            ?? throw new ArgumentNullException(nameof(requestProcessor));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Capture Request Workerを開始しました。");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var item = await _requestQueue.DequeueAsync(stoppingToken);
                var request = item.Request;

                _logger.LogInformation(
                    "Capture Requestを受信しました。RequestId: {RequestId}, CaptureIndex: {CaptureIndex}",
                    request.RequestId,
                    request.CaptureIndex);

                try
                {
                    var result = await _requestProcessor.ProcessAsync(
                        request,
                        stoppingToken);

                    item.TrySetResult(result);

                    _logger.Log(
                        result.Succeeded ? LogLevel.Information : LogLevel.Warning,
                        "Capture Requestを処理しました。RequestId: {RequestId}, CaptureIndex: {CaptureIndex}, Succeeded: {Succeeded}, ErrorCode: {ErrorCode}",
                        result.RequestId,
                        result.CaptureIndex,
                        result.Succeeded,
                        result.ErrorCode);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    item.TrySetCanceled(stoppingToken);
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Capture Requestの処理中にエラーが発生しました。RequestId: {RequestId}, CaptureIndex: {CaptureIndex}",
                        request.RequestId,
                        request.CaptureIndex);

                    item.TrySetResult(
                        new CaptureResult(
                            RequestId: request.RequestId,
                            CaptureIndex: request.CaptureIndex,
                            Succeeded: false,
                            CompletedAt: DateTimeOffset.Now,
                            ErrorCode: "CAPTURE_PROCESSING_FAILED",
                            ErrorMessage:
                                "Capture Requestの処理中にエラーが発生しました。"));
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Capture Request Workerの停止要求を受信しました。");
        }
        finally
        {
            _logger.LogInformation("Capture Request Workerを停止しました。");
        }
    }
}
