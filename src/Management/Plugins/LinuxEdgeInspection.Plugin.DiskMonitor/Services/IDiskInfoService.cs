using LinuxEdgeInspection.Plugin.DiskMonitor.Models;

namespace LinuxEdgeInspection.Plugin.DiskMonitor.Services;

public interface IDiskInfoService
{
    IReadOnlyList<DiskInfo> GetDisks();
}
