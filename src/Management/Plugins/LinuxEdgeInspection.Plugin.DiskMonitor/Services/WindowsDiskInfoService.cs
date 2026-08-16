using LinuxEdgeInspection.Plugin.DiskMonitor.Models;

namespace LinuxEdgeInspection.Plugin.DiskMonitor.Services;

public sealed class WindowsDiskInfoService : IDiskInfoService
{
    public IReadOnlyList<DiskInfo> GetDisks() =>
        DriveInfo.GetDrives()
            .Where(drive => drive.IsReady)
            .Select(drive => new DiskInfo
            {
                Name = drive.Name,
                TotalSize = drive.TotalSize,
                FreeSpace = drive.AvailableFreeSpace
            })
            .ToList();
}
