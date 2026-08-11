using LinuxEdgeInspection.CaptureRequestListener.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LinuxEdgeInspection.CaptureRequestListener.Workers;

/// <summary>
/// Capture Request Queueを監視し、
/// 要求を受信順に1件ずつ処理します。
/// </summary>
/// <remarks>
/// 本Workerは、Queueへ登録されたCapture Requestを順番に取り出し、
/// <see cref="ICaptureRequestProcessor"/>へ処理を委譲します。
///
/// Capture Requestの生成元がPLCであるか、
/// Inspection Worker等の別コンポーネントであるかは意識しません。
///
/// 将来的には、Inspection WorkerからIPC等で受信したCapture Requestも、
/// 同じQueueを経由して処理する構成を想定します。
/// </remarks>
public sealed class CaptureRequestWorker
    : BackgroundService
{
    private readonly ICaptureRequestQueue _requestQueue;
    private readonly ICaptureRequestProcessor _requestProcessor;
    private readonly ILogger<CaptureRequestWorker> _logger;

    /// <summary>
    /// <see cref="CaptureRequestWorker"/>を初期化します。
    /// </summary>
    /// <param name="requestQueue">
    /// Capture Requestを保持するQueueです。
    /// </param>
    /// <param name="requestProcessor">
    /// Capture Requestを1件ずつ処理するサービスです。
    /// </param>
    /// <param name="logger">
    /// ロガーです。
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="requestQueue"/>、
    /// <paramref name="requestProcessor"/>、
    /// または<paramref name="logger"/>が
    /// <see langword="null"/>の場合にスローされます。
    /// </exception>
    public CaptureRequestWorker(
        ICaptureRequestQueue requestQueue,
        ICaptureRequestProcessor requestProcessor,
        ILogger<CaptureRequestWorker> logger)
    {
        _requestQueue = requestQueue
            ?? throw new ArgumentNullException(
                nameof(requestQueue));

        _requestProcessor = requestProcessor
            ?? throw new ArgumentNullException(
                nameof(requestProcessor));

        _logger = logger
            ?? throw new ArgumentNullException(
                nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Capture Request Workerを開始しました。");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Queueへ登録されたCapture Requestを
                // 受信順に1件取り出します。
                var request =
                    await _requestQueue.DequeueAsync(
                        stoppingToken);

                _logger.LogInformation(
                    "Capture Requestを受信しました。RequestId: {RequestId}",
                    request.RequestId);

                try
                {
                    // Capture Requestを処理し、
                    // Runtime実行結果を反映したCaptureResultを受け取ります。
                    var result =
                        await _requestProcessor.ProcessAsync(
                            request,
                            stoppingToken);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation(
                            "Capture Requestの処理が完了しました。"
                            + " RequestId: {RequestId}",
                            result.RequestId);
                    }
                    else
                    {
                        // Runtime自体は起動できたものの、
                        // 撮影処理等が正常完了しなかった場合は
                        // CaptureResultの内容をログへ記録します。
                        _logger.LogWarning(
                            "Capture Requestの処理に失敗しました。"
                            + " RequestId: {RequestId},"
                            + " ErrorCode: {ErrorCode},"
                            + " ErrorMessage: {ErrorMessage}",
                            result.RequestId,
                            result.ErrorCode,
                            result.ErrorMessage);
                    }

                    // 現時点ではCaptureResultをログへ記録するところまでとします。
                    //
                    // 将来的には、この結果をIPC等を介して
                    // Inspection Workerへ返却する処理を追加する想定です。
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    // サービス停止要求によるキャンセルは
                    // 外側の処理へ伝播させます。
                    throw;
                }
                catch (Exception exception)
                {
                    // 1件のCapture Request処理で例外が発生しても、
                    // Workerそのものは停止させず、
                    // 次のCapture Requestを処理できるようにします。
                    _logger.LogError(
                        exception,
                        "Capture Requestの処理中にエラーが発生しました。"
                        + " RequestId: {RequestId}",
                        request.RequestId);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Capture Request Workerの停止要求を受信しました。");
        }
        finally
        {
            _logger.LogInformation(
                "Capture Request Workerを停止しました。");
        }
    }
}