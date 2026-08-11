namespace LinuxEdgeInspection.Camera.V4L2.Models;

/// <summary>
/// 外部プロセスの実行要求を表します。
/// </summary>
/// <param name="FileName">
/// 実行するコマンドまたは実行ファイルのパスです。
/// </param>
/// <param name="Arguments">
/// 実行時に渡す引数一覧です。
/// </param>
/// <param name="Timeout">
/// プロセスの最大実行時間です。
/// </param>
public sealed record ProcessExecutionRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout);