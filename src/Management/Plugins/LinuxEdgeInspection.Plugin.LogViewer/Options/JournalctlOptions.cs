namespace LinuxEdgeInspection.Plugin.LogViewer.Options;

/// <summary>
/// journalctlを使用してjournaldログを取得する際の設定を表します。
/// </summary>
public sealed class JournalctlOptions
{
    /// <summary>
    /// appsettings.jsonで使用する設定セクション名。
    /// </summary>
    public const string SectionName = "LogViewer:Journalctl";

    /// <summary>
    /// 実行するjournalctlコマンドのパスを取得または設定します。
    /// </summary>
    public string ExecutablePath { get; set; } = "/usr/bin/journalctl";

    /// <summary>
    /// journalctlによるログ取得処理のタイムアウト時間（秒）を取得または設定します。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}