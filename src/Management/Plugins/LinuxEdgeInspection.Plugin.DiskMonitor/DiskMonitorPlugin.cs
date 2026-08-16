using LinuxEdgeInspection.Management.Abstractions;

namespace LinuxEdgeInspection.Plugin.DiskMonitor;

public sealed class DiskMonitorPlugin : PluginBase<DiskMonitorPlugin>
{
    protected override string Name => "Disk Monitor";

    protected override string Description => "Displays local disk capacity and usage.";

    protected override PluginIcon Icon => PluginIcon.DiskMonitor;
}
