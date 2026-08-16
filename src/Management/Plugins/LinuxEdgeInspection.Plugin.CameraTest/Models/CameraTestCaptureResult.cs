namespace LinuxEdgeInspection.Plugin.CameraTest.Models;

/// <summary>
/// Camera Testで実行した1回分の撮像結果を表します。
/// </summary>
/// <param name="CaptureSucceeded">撮像処理が成功したかどうか。</param>
/// <param name="CaptureIndex">Inspection内での撮像番号。</param>
/// <param name="FilePath">取得した画像ファイルの保存先パス。</param>
/// <param name="ViewUrl">ブラウザから画像を確認するためのURL。</param>
public sealed record CameraTestCaptureResult(
    bool CaptureSucceeded,
    int CaptureIndex,
    string? FilePath,
    string? ViewUrl);