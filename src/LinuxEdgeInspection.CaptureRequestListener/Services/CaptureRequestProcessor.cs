using LinuxEdgeInspection.Contracts.Capture;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// Queueから受信したCapture Requestに対してRuntimeを1回起動します。
/// </summary>
public sealed class CaptureRequestProcessor : ICaptureRequestProcessor
{
    private readonly ICaptureRuntimeLauncher _runtimeLauncher;

    public CaptureRequestProcessor(ICaptureRuntimeLauncher runtimeLauncher)
    {
        _runtimeLauncher = runtimeLauncher
            ?? throw new ArgumentNullException(nameof(runtimeLauncher));
    }

    public async Task<CaptureResult> ProcessAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var launchResult = await _runtimeLauncher.LaunchAsync(
            cancellationToken);

        return new CaptureResult(
            RequestId: request.RequestId,
            CaptureIndex: request.CaptureIndex,
            Succeeded: launchResult.Succeeded,
            CompletedAt: launchResult.CompletedAt,
            FilePath: launchResult.FilePath,
            ErrorCode: launchResult.ErrorCode,
            ErrorMessage: launchResult.ErrorMessage);
    }
}
