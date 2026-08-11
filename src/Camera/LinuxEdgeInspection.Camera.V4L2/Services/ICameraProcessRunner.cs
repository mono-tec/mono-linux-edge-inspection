using LinuxEdgeInspection.Camera.V4L2.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// 外部プロセスを実行する機能を定義します。
/// </summary>
public interface ICameraProcessRunner
{
    /// <summary>
    /// 指定された条件で外部プロセスを実行します。
    /// </summary>
    /// <param name="request">
    /// 実行ファイル、引数、タイムアウトを含む実行要求です。
    /// </param>
    /// <param name="cancellationToken">
    /// 呼び出し元からのキャンセルを通知するトークンです。
    /// </param>
    /// <returns>
    /// 終了コード、標準出力、標準エラー、実行時間などを含む結果です。
    /// </returns>
    Task<ProcessExecutionResult> ExecuteAsync(
        ProcessExecutionRequest request,
        CancellationToken cancellationToken = default);
}