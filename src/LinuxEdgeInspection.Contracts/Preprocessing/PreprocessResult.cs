namespace LinuxEdgeInspection.Contracts.Preprocessing;

/// <summary>
/// 画像の前処理結果を表します。
/// </summary>
/// <param name="Succeeded">
/// 前処理が正常に完了した場合は <see langword="true"/>、失敗した場合は <see langword="false"/>。
/// </param>
/// <param name="FilePaths">
/// 前処理によって生成された画像ファイルのパス一覧。
/// 1つの入力画像から複数の画像が生成される場合があります。
/// </param>
/// <param name="ErrorCode">
/// 前処理に失敗した場合のエラーコード。
/// 正常終了時は <see langword="null"/>。
/// </param>
/// <param name="ErrorMessage">
/// 前処理に失敗した場合のエラー内容を示すメッセージ。
/// 正常終了時は <see langword="null"/>。
/// </param>
public sealed record PreprocessResult(
    bool Succeeded,
    IReadOnlyList<string> FilePaths,
    string? ErrorCode,
    string? ErrorMessage);