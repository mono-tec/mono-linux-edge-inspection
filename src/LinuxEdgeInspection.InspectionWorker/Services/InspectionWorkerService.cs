using LinuxEdgeInspection.Contracts.Capture;
using Microsoft.Extensions.Logging;

namespace LinuxEdgeInspection.InspectionWorker.Services;

/// <summary>
/// Capture Requestの送信とResult処理だけを担う初期Inspection Workerです。
/// </summary>
public sealed class InspectionWorkerService
{
    private readonly ICaptureRequestClient _captureRequestClient;
    private readonly ILogger<InspectionWorkerService> _logger;

    public InspectionWorkerService(
        ICaptureRequestClient captureRequestClient,
        ILogger<InspectionWorkerService> logger)
    {
        _captureRequestClient = captureRequestClient
            ?? throw new ArgumentNullException(nameof(captureRequestClient));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CaptureResult> CaptureAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _captureRequestClient.SendAsync(
            request,
            cancellationToken);

        _logger.Log(
            result.Succeeded ? LogLevel.Information : LogLevel.Warning,
            "Capture Resultを受信しました。RequestId: {RequestId}, CaptureIndex: {CaptureIndex}, Succeeded: {Succeeded}, ErrorCode: {ErrorCode}",
            result.RequestId,
            result.CaptureIndex,
            result.Succeeded,
            result.ErrorCode);

        return result;
    }
}
