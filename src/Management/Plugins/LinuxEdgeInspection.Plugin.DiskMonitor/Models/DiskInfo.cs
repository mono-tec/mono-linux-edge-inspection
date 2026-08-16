namespace LinuxEdgeInspection.Plugin.DiskMonitor.Models;

/// <summary>
/// ディスクの使用状況を表します。
/// </summary>
public sealed class DiskInfo
{
    /// <summary>
    /// ディスクまたはマウントポイントの名称を取得します。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// ディスクの総容量（バイト）を取得します。
    /// </summary>
    public long TotalSize { get; init; }

    /// <summary>
    /// ディスクの空き容量（バイト）を取得します。
    /// </summary>
    public long FreeSpace { get; init; }

    /// <summary>
    /// ディスクの使用済み容量（バイト）を取得します。
    /// </summary>
    public long UsedSize => TotalSize - FreeSpace;

    /// <summary>
    /// ディスクの使用率（パーセント）を取得します。
    /// </summary>
    public double UsedPercent =>
        TotalSize <= 0 ? 0 : (double)UsedSize / TotalSize * 100;
}