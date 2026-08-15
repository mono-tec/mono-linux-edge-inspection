using LinuxEdgeInspection.Contracts.Analysis;

namespace LinuxEdgeInspection.Analyzer.Services;

public interface IAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default);
}
