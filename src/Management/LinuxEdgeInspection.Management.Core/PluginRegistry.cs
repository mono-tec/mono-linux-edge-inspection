using LinuxEdgeInspection.Management.Abstractions;

namespace LinuxEdgeInspection.Management.Core;

/// <summary>
/// Hostに組み込まれた管理画面Pluginを保持します。
/// </summary>
public sealed class PluginRegistry
{
    private readonly List<IManagementPlugin> _plugins = [];

    public IReadOnlyList<IManagementPlugin> Plugins => _plugins;

    public void Add(IManagementPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        _plugins.Add(plugin);
    }
}
