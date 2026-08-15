using LinuxEdgeInspection.Analyzer.Services;
using LinuxEdgeInspection.Contracts.Analysis;

namespace LinuxEdgeInspection.Analyzer.Tests;

public sealed class DummyAnalyzerTests
{
    [Fact]
    public async Task AnalyzeAsync_WhenInputExists_ReturnsFixedOkResult()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            var analyzer = new DummyAnalyzer();

            var result = await analyzer.AnalyzeAsync(
                new AnalysisRequest(new[] { filePath }));

            Assert.True(result.Succeeded);
            Assert.Equal(InspectionJudgement.Ok, result.Judgement);
            Assert.Equal("DUMMY_OK", result.Label);
            Assert.Null(result.Score);
            Assert.Null(result.ErrorCode);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void AnalysisResult_CanRepresentSuccessfulUnknownJudgement()
    {
        var result = new AnalysisResult(
            Succeeded: true,
            Judgement: InspectionJudgement.Unknown,
            Label: null,
            Score: null,
            ErrorCode: null,
            ErrorMessage: null);

        Assert.True(result.Succeeded);
        Assert.Equal(InspectionJudgement.Unknown, result.Judgement);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenInputDoesNotExist_ReturnsInputNotFound()
    {
        var analyzer = new DummyAnalyzer();
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.jpg");

        var result = await analyzer.AnalyzeAsync(
            new AnalysisRequest(new[] { filePath }));

        Assert.False(result.Succeeded);
        Assert.Equal(InspectionJudgement.Unknown, result.Judgement);
        Assert.Equal(AnalysisErrorCodes.InputNotFound, result.ErrorCode);
    }

    [Fact]
    public void AnalysisResult_CanRepresentProcessingFailure()
    {
        var result = new AnalysisResult(
            Succeeded: false,
            Judgement: InspectionJudgement.Unknown,
            Label: null,
            Score: null,
            ErrorCode: AnalysisErrorCodes.Failed,
            ErrorMessage: "failed");

        Assert.False(result.Succeeded);
        Assert.Equal(InspectionJudgement.Unknown, result.Judgement);
        Assert.Equal(AnalysisErrorCodes.Failed, result.ErrorCode);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var analyzer = new DummyAnalyzer();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => analyzer.AnalyzeAsync(
                new AnalysisRequest(new[] { "capture.jpg" }),
                source.Token));
    }
}
