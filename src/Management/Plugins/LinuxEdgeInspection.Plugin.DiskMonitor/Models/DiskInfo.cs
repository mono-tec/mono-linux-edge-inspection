namespace LinuxEdgeInspection.Plugin.DiskMonitor.Models;

public sealed class DiskInfo
{
    public string Name { get; init; } = string.Empty;

    public long TotalSize { get; init; }

    public long FreeSpace { get; init; }

    public long UsedSize => TotalSize - FreeSpace;

    public double UsedPercent => TotalSize <= 0 ? 0 : (double)UsedSize / TotalSize * 100;
}
