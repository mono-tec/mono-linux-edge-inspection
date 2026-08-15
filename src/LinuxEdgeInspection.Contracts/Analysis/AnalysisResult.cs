namespace LinuxEdgeInspection.Contracts.Analysis;

/// <summary>
/// Analyzerによる分析結果を表します。
/// </summary>
/// <param name="Succeeded">
/// 分析処理が正常に完了した場合は <see langword="true"/>、失敗した場合は <see langword="false"/>。
/// </param>
/// <param name="Judgement">
/// 分析結果に基づくInspectionの共通判定。
/// 処理失敗時は <see cref="InspectionJudgement.Unknown"/> を設定します。
/// </param>
/// <param name="Label">
/// Analyzerが返す判定ラベル。
/// 使用しない場合は <see langword="null"/>。
/// </param>
/// <param name="Score">
/// Analyzerが返す判定スコア。
/// 使用しない場合は <see langword="null"/>。
/// </param>
/// <param name="ErrorCode">
/// 分析処理に失敗した場合のエラーコード。
/// 正常終了時は <see langword="null"/>。
/// </param>
/// <param name="ErrorMessage">
/// 分析処理に失敗した場合のエラー内容を示すメッセージ。
/// 正常終了時は <see langword="null"/>。
/// </param>
public sealed record AnalysisResult(
    bool Succeeded,
    InspectionJudgement Judgement,
    string? Label,
    double? Score,
    string? ErrorCode,
    string? ErrorMessage);