using LinuxEdgeInspection.Contracts.Analysis;

namespace LinuxEdgeInspection.Analyzer.Services;

/// <summary>
/// 有効な入力に対して固定のOK判定を返します。
/// </summary>
public sealed class DummyAnalyzer
    : IAnalyzer
{
    public Task<AnalysisResult> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null ||
            request.FilePaths is null ||
            request.FilePaths.Count == 0 ||
            request.FilePaths.Any(
                filePath =>
                    string.IsNullOrWhiteSpace(filePath) ||
                    !File.Exists(filePath)))
        {
            return Task.FromResult(
                new AnalysisResult(
                    Succeeded: false,
                    Judgement: InspectionJudgement.Unknown,
                    Label: null,
                    Score: null,
                    ErrorCode: AnalysisErrorCodes.InputNotFound,
                    ErrorMessage:
                        "One or more analysis input files were not found."));
        }

        return Task.FromResult(
            new AnalysisResult(
                Succeeded: true,
                Judgement: InspectionJudgement.Ok,
                Label: "DUMMY_OK",
                Score: null,
                ErrorCode: null,
                ErrorMessage: null));
    }
}
