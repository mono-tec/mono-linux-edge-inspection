using LinuxEdgeInspection.Plugin.CameraTest.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxEdgeInspection.Plugin.CameraTest;

/// <summary>
/// Camera Test Pluginのサービス登録を提供します。
/// </summary>
public static class PluginRegistration
{
    public static IServiceCollection AddCameraTestPlugin(this IServiceCollection services)
    {
        services.AddSingleton<ICameraTestService, DummyCameraTestService>();
        return services;
    }
}
