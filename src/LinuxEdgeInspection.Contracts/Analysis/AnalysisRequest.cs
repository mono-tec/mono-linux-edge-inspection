namespace LinuxEdgeInspection.Contracts.Analysis;

/// <summary>
/// Analyzerへ渡す画像ファイル一覧を表します。
/// </summary>
/// <param name="FilePaths">
/// 解析対象となる画像ファイルのパス一覧。
/// Preprocessorの出力結果など、Analyzerが判定に使用する画像を指定します。
/// </param>
public sealed record AnalysisRequest(
    IReadOnlyList<string> FilePaths);