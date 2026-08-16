namespace LinuxEdgeInspection.Plugin.LogViewer.Models;

/// <summary>
/// Log Viewerでログを取得する際の検索条件を表します。
/// </summary>
/// <param name="Application">表示対象とするアプリケーション。</param>
/// <param name="Date">表示対象とする日付。</param>
/// <param name="Level">ログレベルの絞り込み条件。</param>
/// <param name="Cursor">
/// journald上の取得開始位置を示すカーソル。
/// 初回取得時はnullを指定します。
/// </param>
/// <param name="Direction">ログを取得する方向。</param>
public sealed record LogQuery(
    LogApplication Application,
    DateOnly Date,
    LogLevelFilter Level,
    string? Cursor = null,
    LogPageDirection Direction = LogPageDirection.Initial);