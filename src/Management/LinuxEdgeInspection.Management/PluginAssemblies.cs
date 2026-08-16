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
    public static Assembly[] Current => GetForOperatingSystem(
        OperatingSystem.IsLinux());

    public static Assembly[] GetForOperatingSystem(bool isLinux)
    {
        var assemblies = new List<Assembly>
        {
            typeof(DiskMonitorPlugin).Assembly
        };

        if (isLinux)
        {           
            assemblies.Add(typeof(CameraTestPlugin).Assembly);
            assemblies.Add(typeof(LogViewerPlugin).Assembly);
        }

        return assemblies.ToArray();
    }
}
