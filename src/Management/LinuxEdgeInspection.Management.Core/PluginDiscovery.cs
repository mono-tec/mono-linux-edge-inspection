using System.Reflection;
using LinuxEdgeInspection.Management.Abstractions;

namespace LinuxEdgeInspection.Management.Core;

/// <summary>
/// 指定されたAssemblyから管理画面Pluginを検出します。
/// </summary>
public static class PluginDiscovery
{
    public static IReadOnlyList<IManagementPlugin> Discover(params Assembly[] assemblies)
    {
        var plugins = new List<IManagementPlugin>();

        foreach (var assembly in assemblies.Distinct())
        {
            var pluginTypes = assembly
                .GetTypes()
                .Where(type =>
                    typeof(IManagementPlugin).IsAssignableFrom(type) &&
                    type is { IsAbstract: false, IsInterface: false } &&
                    type.GetConstructor(Type.EmptyTypes) is not null);

            foreach (var pluginType in pluginTypes)
            {
                if (Activator.CreateInstance(pluginType) is IManagementPlugin plugin)
                {
                    plugins.Add(plugin);
                }
            }
        }

        return plugins;
    }
}
