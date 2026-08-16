using LinuxEdgeInspection.Plugin.DiskMonitor.Models;

namespace LinuxEdgeInspection.Plugin.DiskMonitor.Services;

public sealed class LinuxDiskInfoService : IDiskInfoService
{
    private static readonly string[] ExcludedMountPrefixes =
    [
        "/proc",
        "/sys",
        "/dev",
        "/run",
        "/etc",
        "/opt/render-ssh"
    ];

    public IReadOnlyList<DiskInfo> GetDisks()
    {
        var disks = new List<DiskInfo>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady || drive.TotalSize <= 0 || IsExcludedMount(drive.Name))
                {
                    continue;
                }

                disks.Add(new DiskInfo
                {
                    Name = drive.Name,
                    TotalSize = drive.TotalSize,
                    FreeSpace = drive.AvailableFreeSpace
                });
            }
            catch
            {
                // Linux環境の仮想マウントには情報取得時に例外となるものがあるため除外します。
            }
        }

        return disks;
    }

    private static bool IsExcludedMount(string mountName) =>
        ExcludedMountPrefixes.Any(prefix =>
            mountName.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
            mountName.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase));
}
