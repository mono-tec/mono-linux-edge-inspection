namespace LinuxEdgeInspection.Camera.Abstractions.Models;

/// <summary>
/// カメラによる静止画取得処理の結果を表します。
/// </summary>
/// <param name="Succeeded">静止画取得処理が成功したかどうか。</param>
/// <param name="FilePath">取得した画像ファイルの保存先パス。</param>
/// <param name="FileSize">取得した画像ファイルのサイズ（バイト）。</param>
/// <param name="CapturedAt">静止画を取得した日時。</param>
/// <param name="Duration">静止画取得処理に要した時間。</param>
/// <param name="DevicePath">撮影に使用したカメラデバイスのパス。</param>
/// <param name="ErrorCode">処理失敗時のエラーコード。</param>
/// <param name="ErrorMessage">処理失敗時のエラーメッセージ。</param>
public sealed record CameraCaptureResult(
    bool Succeeded,
    string? FilePath,
    long FileSize,
    DateTimeOffset CapturedAt,
    TimeSpan Duration,
    string DevicePath,
    string? ErrorCode,
    string? ErrorMessage);