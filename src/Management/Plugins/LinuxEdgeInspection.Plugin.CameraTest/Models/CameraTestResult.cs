namespace LinuxEdgeInspection.Plugin.CameraTest.Models;

public sealed record CameraTestResult(
    string Capture,
    string Preprocess,
    string Analysis,
    string Judgement,
    string Label,
    string? Error);
