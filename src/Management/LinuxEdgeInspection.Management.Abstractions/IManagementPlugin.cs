namespace LinuxEdgeInspection.Management.Abstractions;

/// <summary>
/// 管理画面Pluginが公開する基本情報を定義します。
/// </summary>
public interface IManagementPlugin
{
    PluginManifest Manifest { get; }
}
