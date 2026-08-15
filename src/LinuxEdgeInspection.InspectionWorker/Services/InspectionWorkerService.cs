using LinuxEdgeInspection.Analyzer.Services;
using LinuxEdgeInspection.Contracts.Analysis;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.InspectionWorker.Models;
using LinuxEdgeInspection.Preprocessor.Services;
using Microsoft.Extensions.Logging;

namespace LinuxEdgeInspection.InspectionWorker.Services;

/// <summary>
/// Capture Requestの送信とResult処理だけを担う初期Inspection Workerです。
/// </summary>
public sealed class InspectionWorkerService
{
    private readonly ICaptureRequestClient _captureRequestClient;
    private readonly IPreprocessor _preprocessor;
    private readonly IAnalyzer _analyzer;
    private readonly ILogger<InspectionWorkerService> _logger;

    public InspectionWorkerService(
        ICaptureRequestClient captureRequestClient,
        IPreprocessor preprocessor,
        IAnalyzer analyzer,
        ILogger<InspectionWorkerService> logger)
    {
        _captureRequestClient = captureRequestClient
            ?? throw new ArgumentNullException(nameof(captureRequestClient));
        _preprocessor = preprocessor
            ?? throw new ArgumentNullException(nameof(preprocessor));
        _analyzer = analyzer
            ?? throw new ArgumentNullException(nameof(analyzer));
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
            "Capture Resultを受信しました。RequestId: {RequestId}, CaptureIndex: {CaptureIndex}, Succeeded: {Succeeded}, FilePath: {FilePath}, ErrorCode: {ErrorCode}",
            result.RequestId,
            result.CaptureIndex,
            result.Succeeded,
            result.FilePath,
            result.ErrorCode);

        return result;
    }

    /// <summary>
    /// Capture、Preprocess、Analyzeを順番に1回ずつ実行します。
    /// </summary>
    public async Task<InspectionPipelineResult> InspectOnceAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var captureResult = await CaptureAsync(
            request,
            cancellationToken);

        if (!captureResult.Succeeded)
        {
            return new InspectionPipelineResult(
                captureResult,
                PreprocessResult: null,
                AnalysisResult: null);
        }

        var preprocessResult = await _preprocessor.ProcessAsync(
            captureResult.FilePath ?? string.Empty,
            cancellationToken);

        _logger.Log(
            preprocessResult.Succeeded
                ? LogLevel.Information
                : LogLevel.Warning,
            "Preprocess Resultを受信しました。RequestId: {RequestId}, Succeeded: {Succeeded}, OutputCount: {OutputCount}, ErrorCode: {ErrorCode}",
            captureResult.RequestId,
            preprocessResult.Succeeded,
            preprocessResult.FilePaths.Count,
            preprocessResult.ErrorCode);

        if (!preprocessResult.Succeeded)
        {
            return new InspectionPipelineResult(
                captureResult,
                preprocessResult,
                AnalysisResult: null);
        }

        var analysisRequest = new AnalysisRequest(
            preprocessResult.FilePaths);

        var analysisResult = await _analyzer.AnalyzeAsync(
            analysisRequest,
            cancellationToken);

        _logger.Log(
            analysisResult.Succeeded
                ? LogLevel.Information
                : LogLevel.Warning,
            "Analysis Resultを受信しました。RequestId: {RequestId}, Succeeded: {Succeeded}, Judgement: {Judgement}, Label: {Label}, Score: {Score}, ErrorCode: {ErrorCode}",
            captureResult.RequestId,
            analysisResult.Succeeded,
            analysisResult.Judgement,
            analysisResult.Label,
            analysisResult.Score,
            analysisResult.ErrorCode);

        return new InspectionPipelineResult(
            captureResult,
            preprocessResult,
            analysisResult);
    }
}