using System.Net.Http.Json;
using LinuxEdgeInspection.Plugin.CameraTest.Models;

namespace LinuxEdgeInspection.Plugin.CameraTest.Services;

public sealed class HttpCameraTestService : ICameraTestService
{
    private readonly HttpClient _httpClient;

    public HttpCameraTestService(HttpClient httpClient)
    {
        _httpClient = httpClient
            ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<CameraTestResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            "api/inspection/test",
            content: null,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<CameraTestResponse>(
            cancellationToken)
            ?? throw new InvalidDataException(
                "Management API returned an empty Camera Test response.");

        var captures = dto.Captures.Select(capture =>
            new CameraTestCaptureResult(
                capture.CaptureSucceeded,
                capture.CaptureIndex,
                capture.FilePath,
                capture.FileName is null
                    ? null
                    : $"/inspection/images/{Uri.EscapeDataString(capture.FileName)}"))
            .ToArray();

        return new CameraTestResult(
            dto.RequestId,
            captures,
            captures.Length > 0 &&
            captures.All(capture => capture.CaptureSucceeded)
                ? "Success"
                : "Failed",
            ToStageText(dto.PreprocessSucceeded),
            ToStageText(dto.AnalysisSucceeded),
            dto.Judgement ?? "Not Executed",
            dto.Label,
            FormatError(dto.ErrorCode, dto.ErrorMessage));
    }

    private static string ToStageText(bool? succeeded) => succeeded switch
    {
        true => "Success",
        false => "Failed",
        null => "Not Executed"
    };

    private static string? FormatError(string? code, string? message)
    {
        if (code is null)
        {
            return message;
        }

        return message is null ? code : $"{code}: {message}";
    }

    private sealed record CameraTestResponse(
        string RequestId,
        IReadOnlyList<CameraTestCaptureResponse> Captures,
        bool? PreprocessSucceeded,
        bool? AnalysisSucceeded,
        string? Judgement,
        string? Label,
        string? ErrorCode,
        string? ErrorMessage);

    private sealed record CameraTestCaptureResponse(
        bool CaptureSucceeded,
        int CaptureIndex,
        string? FilePath,
        string? FileName);
}
