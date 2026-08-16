using LinuxEdgeInspection.Management.Abstractions;
using LinuxEdgeInspection.Management.Core;
using LinuxEdgeInspection.Plugin.CameraTest;
using LinuxEdgeInspection.Plugin.DiskMonitor;
using LinuxEdgeInspection.Plugin.LogViewer;

namespace LinuxEdgeInspection.Management.Tests;

public sealed class PluginInfrastructureTests
{
    [Fact]
    public void PluginAssemblies_ForWindows_ExcludesLogViewer()
    {
        var plugins = PluginDiscovery.Discover(
            PluginAssemblies.GetForOperatingSystem(isLinux: false));

        Assert.DoesNotContain(
            plugins,
            plugin => plugin.Manifest.Id == "log-viewer");
    }

    [Fact]
    public void PluginAssemblies_ForLinux_IncludesLogViewer()
    {
        var plugins = PluginDiscovery.Discover(
            PluginAssemblies.GetForOperatingSystem(isLinux: true));

        Assert.Contains(
            plugins,
            plugin => plugin.Manifest.Id == "log-viewer");
    }

    [Fact]
    public void Discover_FindsAllManagementPlugins()
    {
        var plugins = PluginDiscovery.Discover(
            typeof(DiskMonitorPlugin).Assembly,
            typeof(CameraTestPlugin).Assembly,
            typeof(LogViewerPlugin).Assembly);

        Assert.Equal(3, plugins.Count);
        Assert.Equal(
            ["camera-test", "disk-monitor", "log-viewer"],
            plugins.Select(plugin => plugin.Manifest.Id).Order().ToArray());
    }

    [Fact]
    public void Registry_StoresRegisteredPlugins()
    {
        IManagementPlugin[] plugins =
        [
            new DiskMonitorPlugin(),
            new CameraTestPlugin(),
            new LogViewerPlugin()
        ];
        var registry = new PluginRegistry();

        foreach (var plugin in plugins)
        {
            registry.Add(plugin);
        }

        Assert.Equal(plugins, registry.Plugins);
    }

    [Fact]
    public void Plugins_ExposeExpectedManifests()
    {
        AssertManifest(
            new DiskMonitorPlugin().Manifest,
            "disk-monitor",
            "Disk Monitor",
            "/plugins/disk-monitor",
            PluginIcon.DiskMonitor);

        AssertManifest(
            new CameraTestPlugin().Manifest,
            "camera-test",
            "Camera Test",
            "/plugins/camera-test",
            PluginIcon.CameraTest);

        AssertManifest(
            new LogViewerPlugin().Manifest,
            "log-viewer",
            "Log Viewer",
            "/plugins/log-viewer",
            PluginIcon.LogViewer);
    }

    private static void AssertManifest(
        PluginManifest manifest,
        string id,
        string name,
        string route,
        PluginIcon icon)
    {
        Assert.Equal(id, manifest.Id);
        Assert.Equal(name, manifest.Name);
        Assert.Equal(route, manifest.Route);
        Assert.Equal(icon, manifest.Icon);
        Assert.False(string.IsNullOrWhiteSpace(manifest.Description));
        Assert.False(string.IsNullOrWhiteSpace(manifest.Version));
    }
}
