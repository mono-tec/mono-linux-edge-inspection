namespace LinuxEdgeInspection.Plugin.LogViewer.Models;

/// <summary>
/// Log Viewerで表示する1ページ分のログ情報を表します。
/// </summary>
/// <param name="Entries">このページに含まれるログ一覧。</param>
/// <param name="OldestCursor">このページ内で最も古いログのjournaldカーソル。</param>
/// <param name="NewestCursor">このページ内で最も新しいログのjournaldカーソル。</param>
/// <param name="CanLoadOlder">さらに古いログを取得できるかどうか。</param>
/// <param name="CanLoadNewer">さらに新しいログを取得できるかどうか。</param>
public sealed record LogPage(
    IReadOnlyList<LogEntry> Entries,
    string? OldestCursor,
    string? NewestCursor,
    bool CanLoadOlder,
    bool CanLoadNewer)
{
    /// <summary>
    /// ログが存在しない空のページを取得します。
    /// </summary>
    public static LogPage Empty { get; } =
        new([], null, null, false, false);
}