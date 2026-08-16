namespace LinuxEdgeInspection.Plugin.LogViewer.Models;

/// <summary>
/// Log Viewerで使用するログレベルの絞り込み条件を表します。
/// </summary>
public enum LogLevelFilter
{
    /// <summary>
    /// すべてのログを表示します。
    /// </summary>
    All,

    /// <summary>
    /// 情報レベルのログを表示します。
    /// </summary>
    Information,

    /// <summary>
    /// 警告レベルのログを表示します。
    /// </summary>
    Warning,

    /// <summary>
    /// エラーレベルのログを表示します。
    /// </summary>
    Error
}