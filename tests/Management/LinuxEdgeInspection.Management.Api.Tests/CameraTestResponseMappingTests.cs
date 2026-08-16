using LinuxEdgeInspection.Contracts.Analysis;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Contracts.Inspection;
using LinuxEdgeInspection.Contracts.Preprocessing;
using LinuxEdgeInspection.Management.Api.Endpoints;

namespace LinuxEdgeInspection.Management.Api.Tests;

public sealed class CameraTestResponseMappingTests
{
    [Fact]
    public void MapResponse_MapsPipelineAndFilePath()
    {
        var result = new InspectionExecutionResult(
            new CaptureResult(
                "REQ-1",
                1,
                true,
                DateTimeOffset.UtcNow,
                "/var/lib/linux-edge-inspection-runtime/captures/capture-1.jpg",
                null,
                null),
            new PreprocessResult(true, ["processed.jpg"], null, null),
            new AnalysisResult(
                true,
                InspectionJudgement.Ok,
                "OK_LABEL",
                0.9,
                null,
                null));

        var response = InspectionEndpoints.MapResponse(result);

        Assert.Equal("REQ-1", response.RequestId);
        var capture = Assert.Single(response.Captures);
        Assert.True(capture.CaptureSucceeded);
        Assert.Equal(1, capture.CaptureIndex);
        Assert.Equal(
            "/var/lib/linux-edge-inspection-runtime/captures/capture-1.jpg",
            capture.FilePath);
        Assert.Equal("capture-1.jpg", capture.FileName);
        Assert.True(response.PreprocessSucceeded);
        Assert.True(response.AnalysisSucceeded);
        Assert.Equal("Ok", response.Judgement);
        Assert.Equal("OK_LABEL", response.Label);
        Assert.Null(response.ErrorCode);
    }

    [Fact]
    public void MapResponse_WhenCaptureFails_LeavesLaterStagesNull()
    {
        var result = new InspectionExecutionResult(
            new CaptureResult(
                "REQ-2",
                1,
                false,
                DateTimeOffset.UtcNow,
                null,
                "CAPTURE_FAILED",
                "camera unavailable"),
            null,
            null);

        var response = InspectionEndpoints.MapResponse(result);

        Assert.False(Assert.Single(response.Captures).CaptureSucceeded);
        Assert.Null(response.PreprocessSucceeded);
        Assert.Null(response.AnalysisSucceeded);
        Assert.Null(response.Judgement);
        Assert.Equal("CAPTURE_FAILED", response.ErrorCode);
        Assert.Equal("camera unavailable", response.ErrorMessage);
    }
}
