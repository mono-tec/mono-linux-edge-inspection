namespace LinuxEdgeInspection.Plugin.LogViewer.Models;

/// <summary>
/// Log Viewerに表示する1件分のログ情報を表します。
/// </summary>
/// <param name="Timestamp">ログが記録された日時。</param>
/// <param name="Level">ログレベル。</param>
/// <param name="Component">ログを出力したアプリケーションまたはコンポーネント名。</param>
/// <param name="Message">ログ本文。</param>
/// <param name="Cursor">
/// journald上のログ位置を識別するカーソル。
/// 前後のログを取得する際の基準として使用します。
/// </param>
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Component,
    string Message,
    string Cursor = "");