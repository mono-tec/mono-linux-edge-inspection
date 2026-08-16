using System.Net;
using System.Text;
using LinuxEdgeInspection.Plugin.CameraTest.Services;

namespace LinuxEdgeInspection.Management.Tests;

public sealed class HttpCameraTestServiceTests
{
    [Fact]
    public async Task RunAsync_MapsApiResponseAndImageRelayUrl()
    {
        const string json = """
            {
              "requestId": "REQ-HTTP",
              "captures": [
                {
                  "captureSucceeded": true,
                  "captureIndex": 1,
                  "filePath": "/var/lib/linux-edge-inspection-runtime/captures/capture 1.jpg",
                  "fileName": "capture 1.jpg"
                }
              ],
              "preprocessSucceeded": true,
              "analysisSucceeded": true,
              "judgement": "Ok",
              "label": "PASS",
              "errorCode": null,
              "errorMessage": null
            }
            """;
        var client = new HttpClient(new StubHandler(json))
        {
            BaseAddress = new Uri("http://127.0.0.1:8081/")
        };
        var service = new HttpCameraTestService(client);

        var result = await service.RunAsync();

        Assert.Equal("REQ-HTTP", result.RequestId);
        Assert.Equal("Success", result.Capture);
        Assert.Equal("Success", result.Preprocess);
        Assert.Equal("Success", result.Analysis);
        Assert.Equal("Ok", result.Judgement);
        Assert.Equal("PASS", result.Label);
        var capture = Assert.Single(result.Captures);
        Assert.Equal(1, capture.CaptureIndex);
        Assert.Equal(
            "/var/lib/linux-edge-inspection-runtime/captures/capture 1.jpg",
            capture.FilePath);
        Assert.Equal(
            "/inspection/images/capture%201.jpg",
            capture.ViewUrl);
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "http://127.0.0.1:8081/api/inspection/test",
                request.RequestUri?.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }
}
