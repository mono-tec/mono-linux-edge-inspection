using LinuxEdgeInspection.Analyzer.Services;
using LinuxEdgeInspection.Contracts.Analysis;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Contracts.Inspection;
using LinuxEdgeInspection.Contracts.Preprocessing;
using LinuxEdgeInspection.InspectionWorker.Options;
using LinuxEdgeInspection.InspectionWorker.Services;
using LinuxEdgeInspection.Management.Api.Options;
using LinuxEdgeInspection.Management.Api.Services;
using LinuxEdgeInspection.Preprocessor.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.Management.Api.Tests;

public sealed class InspectionWorkerIpcIntegrationTests
{
    [Fact]
    public async Task ClientAndWorkerServer_RunExistingPipelineOverUnixSocket()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"lei-worker-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var socketPath = Path.Combine(directory, "worker.sock");

        var workerService = new InspectionWorkerService(
            new FakeCaptureClient(),
            new FakePreprocessor(),
            new FakeAnalyzer(),
            NullLogger<InspectionWorkerService>.Instance);
        var server = new UnixDomainSocketInspectionRequestServer(
            workerService,
            Microsoft.Extensions.Options.Options.Create(
                new InspectionRequestEndpointOptions
            {
                SocketPath = socketPath,
                Backlog = 4
            }),
            NullLogger<UnixDomainSocketInspectionRequestServer>.Instance);
        var client = new UnixDomainSocketInspectionWorkerClient(
            Microsoft.Extensions.Options.Options.Create(
                new InspectionWorkerClientOptions
            {
                SocketPath = socketPath,
                TimeoutSeconds = 5
            }));

        try
        {
            await server.StartAsync(CancellationToken.None);
            await server.Ready.WaitAsync(TimeSpan.FromSeconds(5));

            var result = await client.ExecuteAsync(
                new InspectionExecutionRequest(
                    "REQ-IPC",
                    1,
                    DateTimeOffset.UtcNow));

            Assert.True(result.CaptureResult.Succeeded);
            Assert.Equal("REQ-IPC", result.CaptureResult.RequestId);
            Assert.Equal("/captures/REQ-IPC.jpg", result.CaptureResult.FilePath);
            Assert.True(result.PreprocessResult?.Succeeded);
            Assert.True(result.AnalysisResult?.Succeeded);
            Assert.Equal(InspectionJudgement.Ok,
                result.AnalysisResult?.Judgement);
        }
        finally
        {
            await server.StopAsync(CancellationToken.None);
            server.Dispose();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class FakeCaptureClient : ICaptureRequestClient
    {
        public Task<CaptureResult> SendAsync(
            CaptureRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CaptureResult(
                request.RequestId,
                request.CaptureIndex,
                true,
                DateTimeOffset.UtcNow,
                $"/captures/{request.RequestId}.jpg",
                null,
                null));
    }

    private sealed class FakePreprocessor : IPreprocessor
    {
        public Task<PreprocessResult> ProcessAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PreprocessResult(
                true,
                [filePath],
                null,
                null));
    }

    private sealed class FakeAnalyzer : IAnalyzer
    {
        public Task<AnalysisResult> AnalyzeAsync(
            AnalysisRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AnalysisResult(
                true,
                InspectionJudgement.Ok,
                "OK",
                1,
                null,
                null));
    }
}
