using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// 外部システムコマンドを実行します。
/// </summary>
public interface ISystemCommandRunner
{
    /// <summary>
    /// 指定されたコマンドを実行します。
    /// </summary>
    /// <param name="fileName">
    /// 実行するコマンドまたは実行ファイルです。
    /// </param>
    /// <param name="arguments">
    /// コマンドへ渡す引数です。
    /// </param>
    /// <param name="timeout">
    /// コマンド実行のタイムアウト時間です。
    /// </param>
    /// <param name="cancellationToken">
    /// 実行処理を中止するためのキャンセルトークンです。
    /// </param>
    /// <returns>
    /// コマンドの実行結果です。
    /// </returns>
    Task<SystemCommandExecutionResult> ExecuteAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}