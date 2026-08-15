namespace LinuxEdgeInspection.Contracts.Analysis;

/// <summary>
/// 検査としての共通判定を表します。
/// </summary>
public enum InspectionJudgement
{
    /// <summary>
    /// 判定不能、または判定結果が確定していない状態。
    /// 処理エラーそのものを表す値ではありません。
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// 検査結果がNGであることを表します。
    /// </summary>
    Ng = 0,

    /// <summary>
    /// 検査結果がOKであることを表します。
    /// </summary>
    Ok = 1
}