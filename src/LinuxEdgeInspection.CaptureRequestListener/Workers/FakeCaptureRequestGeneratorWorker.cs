using LinuxEdgeInspection.CaptureRequestListener.Options;
using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;
using LinuxEdgeInspection.Contracts.Capture;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.CaptureRequestListener.Workers;

/// <summary>
/// 開発・検証用にCapture Requestを自動生成します。
/// </summary>
/// <remarks>
/// 本Workerは実PLCを模擬するものではありません。
///
/// 開発・検証時にCapture Requestを直接Queueへ追加し、
/// CaptureRequestListenerの以下の処理を確認するために使用します。
///
/// <code>
/// FakeCaptureRequestGeneratorWorker
///     ↓
/// CaptureRequestQueue
///     ↓
/// CaptureRequestWorker
///     ↓
/// CaptureRequestProcessor
///     ↓
/// Runtime
/// </code>
///
/// 実運用では、Inspection Workerから受信したCapture Requestを
/// Queueへ追加する経路へ置き換える予定です。
/// </remarks>
public sealed class FakeCaptureRequestGeneratorWorker
    : BackgroundService
{
    private readonly ICaptureRequestQueue _requestQueue;
    private readonly FakeCaptureRequestGeneratorOptions _options;
    private readonly ILogger<FakeCaptureRequestGeneratorWorker> _logger;

    /// <summary>
    /// <see cref="FakeCaptureRequestGeneratorWorker"/> を初期化します。
    /// </summary>
    /// <param name="requestQueue">
    /// Capture Requestを保持するQueueです。
    /// </param>
    /// <param name="options">
    /// Fake Capture Request Generatorの設定です。
    /// </param>
    /// <param name="logger">
    /// ロガーです。
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="requestQueue"/>、
    /// <paramref name="options"/>、
    /// または<paramref name="logger"/>が
    /// <see langword="null"/>の場合にスローされます。
    /// </exception>
    public FakeCaptureRequestGeneratorWorker(
        ICaptureRequestQueue requestQueue,
        IOptions<FakeCaptureRequestGeneratorOptions> options,
        ILogger<FakeCaptureRequestGeneratorWorker> logger)
    {
        _requestQueue = requestQueue
            ?? throw new ArgumentNullException(
                nameof(requestQueue));

        ArgumentNullException.ThrowIfNull(
            options);

        _options = options.Value
            ?? throw new ArgumentNullException(
                nameof(options));

        _logger = logger
            ?? throw new ArgumentNullException(
                nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Fake Capture Request Generatorは無効です。");

            return;
        }

        _logger.LogInformation(
            "Fake Capture Request Generatorを開始しました。");

        try
        {
            // サービス起動直後に要求を発行せず、
            // 他のHostedServiceの起動を待つための初期待機時間です。
            if (_options.InitialDelaySeconds > 0)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        _options.InitialDelaySeconds),
                    stoppingToken);
            }

            for (var index = 0;
                 index < _options.RequestCount;
                 index++)
            {
                stoppingToken.ThrowIfCancellationRequested();

                // StartRequestIdを基準として、
                // 要求ごとに連番のRequestIdを生成します。
                var requestId =
                    _options.StartRequestId + index;

                // 開発・検証用のCapture Requestを生成します。
                var request =
                    new CaptureRequest(
                        RequestId: requestId.ToString(
                            System.Globalization.CultureInfo.InvariantCulture),
                        CaptureIndex: 1,
                        RequestedAt: DateTimeOffset.Now);

                // 旧実装ではFakePlcSignalServiceを経由していましたが、
                // 新構成ではCapture RequestをQueueへ直接追加します。
                //
                // PLC固有の信号受信処理は、
                // 将来Equipment Gateway側で扱うため、
                // CaptureRequestListenerからは分離します。
                await _requestQueue.EnqueueAsync(
                    new CaptureRequestQueueItem(request),
                    stoppingToken);

                _logger.LogInformation(
                    "Fake Capture Requestを追加しました。RequestId: {RequestId}",
                    requestId);

                // 最後の要求を追加した後は、
                // 次の要求がないため待機しません。
                if (index < _options.RequestCount - 1)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(
                            _options.IntervalSeconds),
                        stoppingToken);
                }
            }

            _logger.LogInformation(
                "Fake Capture Requestの追加が完了しました。");
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Fake Capture Request Generatorの停止要求を受信しました。");
        }
        finally
        {
            _logger.LogInformation(
                "Fake Capture Request Generatorを停止しました。");
        }
    }
}
