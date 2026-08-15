using LinuxEdgeInspection.Contracts.Analysis;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Contracts.Preprocessing;

namespace LinuxEdgeInspection.InspectionWorker.Models;

/// <summary>
/// 1回のInspection Pipeline実行結果を表します。
/// </summary>
/// <param name="CaptureResult">
/// Capture処理の結果。
/// </param>
/// <param name="PreprocessResult">
/// Preprocess処理の結果。
/// Capture処理に失敗した場合は <see langword="null"/>。
/// </param>
/// <param name="AnalysisResult">
/// Analyze処理の結果。
/// CaptureまたはPreprocess処理に失敗した場合は <see langword="null"/>。
/// </param>
public sealed record InspectionPipelineResult(
    CaptureResult CaptureResult,
    PreprocessResult? PreprocessResult,
    AnalysisResult? AnalysisResult);
