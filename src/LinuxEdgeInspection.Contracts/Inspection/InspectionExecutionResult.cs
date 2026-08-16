using LinuxEdgeInspection.Contracts.Analysis;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Contracts.Preprocessing;

namespace LinuxEdgeInspection.Contracts.Inspection;

/// <summary>
/// InspectionWorkerが実行した1回のInspection Pipeline結果です。
/// </summary>
/// <param name="CaptureResult">撮像処理の実行結果。</param>
/// <param name="PreprocessResult">
/// 前処理の実行結果。
/// 撮像処理に失敗して前処理が実行されなかった場合はnullになります。
/// </param>
/// <param name="AnalysisResult">
/// 解析処理の実行結果。
/// 撮像処理または前処理に失敗して解析が実行されなかった場合はnullになります。
/// </param>
public sealed record InspectionExecutionResult(
    CaptureResult CaptureResult,
    PreprocessResult? PreprocessResult,
    AnalysisResult? AnalysisResult);