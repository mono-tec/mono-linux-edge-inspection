namespace LinuxEdgeInspection.Plugin.LogViewer.Models;

/// <summary>
/// Log Viewerでログを取得する方向を表します。
/// </summary>
public enum LogPageDirection
{
    /// <summary>
    /// 初回表示として最新側のログを取得します。
    /// </summary>
    Initial,

    /// <summary>
    /// 現在のページより古いログを取得します。
    /// </summary>
    Older,

    /// <summary>
    /// 現在のページより新しいログを取得します。
    /// </summary>
    Newer
}