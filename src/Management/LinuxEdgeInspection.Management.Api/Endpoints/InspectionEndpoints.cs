using System.Net.Sockets;
using LinuxEdgeInspection.Contracts.Inspection;
using LinuxEdgeInspection.Management.Api.Dtos;
using LinuxEdgeInspection.Management.Api.Services;

namespace LinuxEdgeInspection.Management.Api.Endpoints;

public static class InspectionEndpoints
{
    public static IEndpointRouteBuilder MapInspectionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/inspection/test", RunCameraTestAsync);
        endpoints.MapGet(
            "/api/inspection/images/{fileName}",
            GetCaptureImage);
        return endpoints;
    }

    internal static async Task<IResult> RunCameraTestAsync(
        IInspectionWorkerClient workerClient,
        CancellationToken cancellationToken)
    {
        var request = new InspectionExecutionRequest(
            Guid.NewGuid().ToString("D"),
            CaptureIndex: 1,
            RequestedAt: DateTimeOffset.UtcNow);

        try
        {
            var result = await workerClient.ExecuteAsync(
                request,
                cancellationToken);
            return Results.Ok(MapResponse(result));
        }
        catch (TimeoutException exception)
        {
            return Results.Problem(
                exception.Message,
                statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception exception)
            when (exception is SocketException or IOException)
        {
            return Results.Problem(
                "InspectionWorkerへ接続できませんでした。",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    internal static CameraTestResponse MapResponse(
        InspectionExecutionResult result)
    {
        var capture = result.CaptureResult;
        var error = GetError(result);

        return new CameraTestResponse(
            capture.RequestId,
            [new CameraTestCaptureResponse(
                capture.Succeeded,
                capture.CaptureIndex,
                capture.FilePath,
                capture.FilePath is null
                    ? null
                    : Path.GetFileName(capture.FilePath))],
            result.PreprocessResult?.Succeeded,
            result.AnalysisResult?.Succeeded,
            result.AnalysisResult?.Judgement.ToString(),
            result.AnalysisResult?.Label,
            error.ErrorCode,
            error.ErrorMessage);
    }

    internal static IResult GetCaptureImage(
        string fileName,
        CaptureImageService imageService)
    {
        var result = imageService.Open(fileName);
        return result.Status switch
        {
            CaptureImageOpenStatus.Success => Results.Stream(
                result.Stream!,
                "image/jpeg"),
            CaptureImageOpenStatus.NotFound => Results.NotFound(),
            _ => Results.BadRequest()
        };
    }

    private static (string? ErrorCode, string? ErrorMessage) GetError(
        InspectionExecutionResult result)
    {
        if (!result.CaptureResult.Succeeded)
        {
            return (
                result.CaptureResult.ErrorCode,
                result.CaptureResult.ErrorMessage);
        }

        if (result.PreprocessResult is { Succeeded: false } preprocess)
        {
            return (preprocess.ErrorCode, preprocess.ErrorMessage);
        }

        if (result.AnalysisResult is { Succeeded: false } analysis)
        {
            return (analysis.ErrorCode, analysis.ErrorMessage);
        }

        return (null, null);
    }
}
