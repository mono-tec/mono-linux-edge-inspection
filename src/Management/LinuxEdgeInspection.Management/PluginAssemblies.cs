using System.Reflection;
using LinuxEdgeInspection.Plugin.CameraTest;
using LinuxEdgeInspection.Plugin.DiskMonitor;
using LinuxEdgeInspection.Plugin.LogViewer;

namespace LinuxEdgeInspection.Management;

/// <summary>
/// Hostへ静的に組み込むPlugin Assemblyを一元管理します。
/// </summary>
public static class PluginAssemblies
{
    public static readonly Assembly[] All =
    [
        typeof(DiskMonitorPlugin).Assembly,
        typeof(CameraTestPlugin).Assembly,
        typeof(LogViewerPlugin).Assembly
    ];
}
