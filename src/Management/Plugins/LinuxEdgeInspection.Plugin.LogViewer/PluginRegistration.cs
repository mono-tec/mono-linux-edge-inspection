using LinuxEdgeInspection.Plugin.LogViewer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxEdgeInspection.Plugin.LogViewer;

/// <summary>
/// Log Viewer Pluginのサービス登録を提供します。
/// </summary>
public static class PluginRegistration
{
    public static IServiceCollection AddLogViewerPlugin(this IServiceCollection services)
    {
        services.AddSingleton<ILogViewerService, DummyLogViewerService>();
        return services;
    }
}
