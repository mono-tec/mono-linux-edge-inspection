using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// Queueから受信したCapture Requestを処理します。
/// </summary>
/// <remarks>
/// CaptureRequestProcessorは、撮影要求に対してRuntimeを起動し、
/// その実行結果からCaptureResultを生成します。
///
/// 旧実装ではPLCへの結果返却も担当していましたが、
/// 新構成ではPLC固有処理をCaptureRequestListenerから分離します。
///
/// 将来的には、生成したCaptureResultをIPC等を介して
/// Inspection Workerへ返却する構成を想定します。
/// </remarks>
public sealed class CaptureRequestProcessor
    : ICaptureRequestProcessor
{
    private readonly ICaptureRuntimeLauncher _runtimeLauncher;

    /// <summary>
    /// <see cref="CaptureRequestProcessor"/> を初期化します。
    /// </summary>
    /// <param name="runtimeLauncher">
    /// 撮影Runtimeを起動するサービスです。
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="runtimeLauncher"/> が
    /// <see langword="null"/> の場合にスローされます。
    /// </exception>
    public CaptureRequestProcessor(
        ICaptureRuntimeLauncher runtimeLauncher)
    {
        _runtimeLauncher = runtimeLauncher
            ?? throw new ArgumentNullException(
                nameof(runtimeLauncher));
    }

    /// <summary>
    /// Capture Requestを処理します。
    /// </summary>
    /// <param name="request">
    /// 処理対象のCapture Requestです。
    /// </param>
    /// <param name="cancellationToken">
    /// 処理のキャンセルを通知するトークンです。
    /// </param>
    /// <returns>
    /// Runtime実行結果を反映したCapture Resultです。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> が
    /// <see langword="null"/> の場合にスローされます。
    /// </exception>
    public async Task<CaptureResult> ProcessAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        // Runtimeを1回起動し、1回の撮影処理を実行します。
        var launchResult =
            await _runtimeLauncher.LaunchAsync(
                cancellationToken);

        // Runtimeの実行結果から、
        // CaptureRequestListener内部で扱うCapture Resultを生成します。
        //
        // 現時点ではPLC等の外部設備へ直接結果を返却しません。
        // 将来的にはInspection WorkerへのIPC返却処理を
        // 別の責務として追加する想定です。
        return new CaptureResult(
            RequestId: request.RequestId,
            Succeeded: launchResult.Succeeded,
            CompletedAt: launchResult.CompletedAt,
            ErrorCode: launchResult.ErrorCode,
            ErrorMessage: launchResult.ErrorMessage);
    }
}