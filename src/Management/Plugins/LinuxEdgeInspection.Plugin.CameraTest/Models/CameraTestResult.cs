namespace LinuxEdgeInspection.Plugin.CameraTest.Models;

/// <summary>
/// Camera Testで実行した検査処理の結果を表します。
/// </summary>
/// <param name="RequestId">Inspection要求を一意に識別するID。</param>
/// <param name="Captures">Camera Testで実行した撮像結果の一覧。</param>
/// <param name="Capture">撮像処理全体の結果。</param>
/// <param name="Preprocess">前処理の結果。</param>
/// <param name="Analysis">解析処理の結果。</param>
/// <param name="Judgement">検査判定結果。</param>
/// <param name="Label">解析結果に付与されたラベル。</param>
/// <param name="Error">処理中に発生したエラー内容。</param>
public sealed record CameraTestResult(
    string RequestId,
    IReadOnlyList<CameraTestCaptureResult> Captures,
    string Capture,
    string Preprocess,
    string Analysis,
    string Judgement,
    string Label,
    string? Error);