using LinuxEdgeInspection.Analyzer.Services;
using LinuxEdgeInspection.Contracts.Analysis;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Contracts.Preprocessing;
using LinuxEdgeInspection.InspectionWorker.Services;
using LinuxEdgeInspection.Preprocessor.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LinuxEdgeInspection.InspectionWorker.Tests;

public sealed class InspectionWorkerServiceTests
{
    [Fact]
    public async Task InspectOnceAsync_RunsStagesInOrderAndReturnsAnalysisResult()
    {
        var calls = new List<string>();
        var captureResult = CreateCaptureResult(succeeded: true);
        var preprocessResult = new PreprocessResult(
            Succeeded: true,
            FilePaths: new[] { "processed-1.jpg", "processed-2.jpg" },
            ErrorCode: null,
            ErrorMessage: null);
        var analysisResult = CreateAnalysisResult();
        var captureClient = new FakeCaptureRequestClient(
            captureResult,
            calls);
        var preprocessor = new FakePreprocessor(
            preprocessResult,
            calls);
        var analyzer = new FakeAnalyzer(
            analysisResult,
            calls);
        var service = CreateService(
            captureClient,
            preprocessor,
            analyzer);

        var result = await service.InspectOnceAsync(CreateRequest());

        Assert.Equal(new[] { "Capture", "Preprocess", "Analyze" }, calls);
        Assert.Same(captureResult, result.CaptureResult);
        Assert.Same(preprocessResult, result.PreprocessResult);
        Assert.Same(analysisResult, result.AnalysisResult);
        Assert.Equal(captureResult.FilePath, preprocessor.LastFilePath);
        Assert.NotNull(analyzer.LastRequest);
        Assert.Same(
            preprocessResult.FilePaths,
            analyzer.LastRequest.FilePaths);
    }

    [Fact]
    public async Task InspectOnceAsync_WhenCaptureFails_DoesNotRunLaterStages()
    {
        var calls = new List<string>();
        var captureClient = new FakeCaptureRequestClient(
            CreateCaptureResult(succeeded: false),
            calls);
        var preprocessor = new FakePreprocessor(
            CreatePreprocessResult(),
            calls);
        var analyzer = new FakeAnalyzer(
            CreateAnalysisResult(),
            calls);
        var service = CreateService(
            captureClient,
            preprocessor,
            analyzer);

        var result = await service.InspectOnceAsync(CreateRequest());

        Assert.Equal(new[] { "Capture" }, calls);
        Assert.Null(result.PreprocessResult);
        Assert.Null(result.AnalysisResult);
    }

    [Fact]
    public async Task InspectOnceAsync_WhenPreprocessFails_DoesNotRunAnalyzer()
    {
        var calls = new List<string>();
        var failedPreprocessResult = new PreprocessResult(
            Succeeded: false,
            FilePaths: Array.Empty<string>(),
            ErrorCode: PreprocessErrorCodes.InputNotFound,
            ErrorMessage: "not found");
        var captureClient = new FakeCaptureRequestClient(
            CreateCaptureResult(succeeded: true),
            calls);
        var preprocessor = new FakePreprocessor(
            failedPreprocessResult,
            calls);
        var analyzer = new FakeAnalyzer(
            CreateAnalysisResult(),
            calls);
        var service = CreateService(
            captureClient,
            preprocessor,
            analyzer);

        var result = await service.InspectOnceAsync(CreateRequest());

        Assert.Equal(new[] { "Capture", "Preprocess" }, calls);
        Assert.Same(failedPreprocessResult, result.PreprocessResult);
        Assert.Null(result.AnalysisResult);
    }

    [Fact]
    public async Task InspectOnceAsync_PassesCancellationTokenToEveryStage()
    {
        var calls = new List<string>();
        var captureClient = new FakeCaptureRequestClient(
            CreateCaptureResult(succeeded: true),
            calls);
        var preprocessor = new FakePreprocessor(
            CreatePreprocessResult(),
            calls);
        var analyzer = new FakeAnalyzer(
            CreateAnalysisResult(),
            calls);
        var service = CreateService(
            captureClient,
            preprocessor,
            analyzer);
        using var source = new CancellationTokenSource();

        await service.InspectOnceAsync(CreateRequest(), source.Token);

        Assert.Equal(source.Token, captureClient.LastToken);
        Assert.Equal(source.Token, preprocessor.LastToken);
        Assert.Equal(source.Token, analyzer.LastToken);
    }

    private static InspectionWorkerService CreateService(
        ICaptureRequestClient captureClient,
        IPreprocessor preprocessor,
        IAnalyzer analyzer) =>
        new(
            captureClient,
            preprocessor,
            analyzer,
            NullLogger<InspectionWorkerService>.Instance);

    private static CaptureRequest CreateRequest() =>
        new(
            RequestId: "REQ-001",
            CaptureIndex: 1,
            RequestedAt: DateTimeOffset.UtcNow);

    private static CaptureResult CreateCaptureResult(bool succeeded) =>
        new(
            RequestId: "REQ-001",
            CaptureIndex: 1,
            Succeeded: succeeded,
            CompletedAt: DateTimeOffset.UtcNow,
            FilePath: succeeded ? "capture.jpg" : null,
            ErrorCode: succeeded ? null : "CAPTURE_FAILED",
            ErrorMessage: succeeded ? null : "failed");

    private static PreprocessResult CreatePreprocessResult() =>
        new(
            Succeeded: true,
            FilePaths: new[] { "capture.jpg" },
            ErrorCode: null,
            ErrorMessage: null);

    private static AnalysisResult CreateAnalysisResult() =>
        new(
            Succeeded: true,
            Judgement: InspectionJudgement.Ok,
            Label: "DUMMY_OK",
            Score: null,
            ErrorCode: null,
            ErrorMessage: null);

    private sealed class FakeCaptureRequestClient
        : ICaptureRequestClient
    {
        private readonly CaptureResult _result;
        private readonly List<string> _calls;

        public FakeCaptureRequestClient(
            CaptureResult result,
            List<string> calls)
        {
            _result = result;
            _calls = calls;
        }

        public CancellationToken LastToken { get; private set; }

        public Task<CaptureResult> SendAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Capture");
            LastToken = cancellationToken;
            return Task.FromResult(_result);
        }
    }

    private sealed class FakePreprocessor
        : IPreprocessor
    {
        private readonly PreprocessResult _result;
        private readonly List<string> _calls;

        public FakePreprocessor(
            PreprocessResult result,
            List<string> calls)
        {
            _result = result;
            _calls = calls;
        }

        public string? LastFilePath { get; private set; }

        public CancellationToken LastToken { get; private set; }

        public Task<PreprocessResult> ProcessAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Preprocess");
            LastFilePath = filePath;
            LastToken = cancellationToken;
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeAnalyzer
        : IAnalyzer
    {
        private readonly AnalysisResult _result;
        private readonly List<string> _calls;

        public FakeAnalyzer(
            AnalysisResult result,
            List<string> calls)
        {
            _result = result;
            _calls = calls;
        }

        public AnalysisRequest? LastRequest { get; private set; }

        public CancellationToken LastToken { get; private set; }

        public Task<AnalysisResult> AnalyzeAsync(
            AnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Analyze");
            LastRequest = request;
            LastToken = cancellationToken;
            return Task.FromResult(_result);
        }
    }
}
