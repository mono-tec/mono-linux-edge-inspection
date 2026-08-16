namespace LinuxEdgeInspection.Management.Abstractions;

/// <summary>
/// Pluginのマニフェスト生成を共通化します。
/// </summary>
public abstract class PluginBase<TPlugin> : IManagementPlugin
{
    protected abstract string Name { get; }

    protected abstract string Description { get; }

    protected abstract PluginIcon Icon { get; }

    public PluginManifest Manifest =>
        PluginManifest.Create<TPlugin>(Name, Description, Icon);
}
