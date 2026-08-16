using LinuxEdgeInspection.Plugin.CameraTest;
using LinuxEdgeInspection.Plugin.CameraTest.Services;
using LinuxEdgeInspection.Plugin.DiskMonitor;
using LinuxEdgeInspection.Plugin.DiskMonitor.Services;
using LinuxEdgeInspection.Plugin.LogViewer;
using LinuxEdgeInspection.Plugin.LogViewer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxEdgeInspection.Management.Tests;

public sealed class PluginRegistrationTests
{
    [Fact]
    public void AddDiskMonitorPlugin_RegistersPlatformDiskService()
    {
        var services = new ServiceCollection();

        services.AddDiskMonitorPlugin();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IDiskInfoService>();

        if (OperatingSystem.IsWindows())
        {
            Assert.IsType<WindowsDiskInfoService>(service);
        }
        else
        {
            Assert.IsType<LinuxDiskInfoService>(service);
        }
    }

    [Fact]
    public void AddCameraTestPlugin_RegistersCameraTestService()
    {
        var services = new ServiceCollection();

        services.AddCameraTestPlugin();

        using var provider = services.BuildServiceProvider();

        Assert.IsAssignableFrom<ICameraTestService>(
            provider.GetRequiredService<ICameraTestService>());
    }

    [Fact]
    public void AddLogViewerPlugin_RegistersJournaldLogViewerService()
    {
        var services = new ServiceCollection();

        services.AddLogViewerPlugin();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<JournaldLogViewerService>(
            provider.GetRequiredService<ILogViewerService>());
    }
}