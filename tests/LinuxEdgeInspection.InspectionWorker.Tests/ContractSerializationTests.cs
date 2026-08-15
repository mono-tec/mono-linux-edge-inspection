using System.Text.Json;
using LinuxEdgeInspection.Contracts.Capture;
using LinuxEdgeInspection.Ipc.Serialization;

namespace LinuxEdgeInspection.InspectionWorker.Tests;

public sealed class ContractSerializationTests
{
    [Fact]
    public void CaptureRequest_RoundTripsAsCamelCaseJson()
    {
        var expected = new CaptureRequest(
            "REQ-001",
            1,
            DateTimeOffset.Parse("2026-08-15T10:00:00+09:00"));

        var json = JsonSerializer.Serialize(
            expected,
            LengthPrefixedJsonMessageFraming.SerializerOptions);
        var actual = JsonSerializer.Deserialize<CaptureRequest>(
            json,
            LengthPrefixedJsonMessageFraming.SerializerOptions);

        Assert.Contains("\"requestId\"", json);
        Assert.Contains("\"captureIndex\"", json);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CaptureResult_RoundTripsAsCamelCaseJson()
    {
        var expected = new CaptureResult(
            RequestId:"REQ-001",
            CaptureIndex: 1,
            Succeeded:  false,
            CompletedAt:  DateTimeOffset.Parse("2026-08-15T10:00:01+09:00"),
            FilePath: null,
            ErrorCode: "CAPTURE_RUNTIME_LAUNCH_FAILED",
            ErrorMessage: "failed");

        var json = JsonSerializer.Serialize(
            expected,
            LengthPrefixedJsonMessageFraming.SerializerOptions);
        var actual = JsonSerializer.Deserialize<CaptureResult>(
            json,
            LengthPrefixedJsonMessageFraming.SerializerOptions);

        Assert.Contains("\"succeeded\"", json);
        Assert.Equal(expected, actual);
    }
}
