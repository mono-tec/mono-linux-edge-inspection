using LinuxEdgeInspection.Plugin.DiskMonitor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxEdgeInspection.Plugin.DiskMonitor;

/// <summary>
/// Disk Monitor Pluginのサービス登録を提供します。
/// </summary>
public static class PluginRegistration
{
    public static IServiceCollection AddDiskMonitorPlugin(this IServiceCollection services)
    {
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IDiskInfoService, WindowsDiskInfoService>();
        }
        else
        {
            services.AddSingleton<IDiskInfoService, LinuxDiskInfoService>();
        }

        return services;
    }
}
