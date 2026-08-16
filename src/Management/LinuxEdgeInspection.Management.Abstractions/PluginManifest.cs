using System.Reflection;

namespace LinuxEdgeInspection.Management.Abstractions;

/// <summary>
/// HostがPluginのNavigationと画面ルートを構成するための情報です。
/// </summary>
public sealed record PluginManifest(
    string Id,
    string Name,
    string Description,
    string Version,
    string Route,
    PluginIcon Icon)
{
    public static PluginManifest Create<TPlugin>(
        string name,
        string description,
        PluginIcon icon)
    {
        var assembly = typeof(TPlugin).Assembly;
        var id = CreatePluginId(assembly);

        return new PluginManifest(
            Id: id,
            Name: name,
            Description: description,
            Version: GetDisplayVersion(assembly),
            Route: $"/plugins/{id}",
            Icon: icon);
    }

    private static string CreatePluginId(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        var pluginName = assemblyName
            .Replace("LinuxEdgeInspection.Plugin.", string.Empty)
            .Replace("Plugin", string.Empty);

        return ToKebabCase(pluginName);
    }

    private static string GetDisplayVersion(Assembly assembly)
    {
        var version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "0.1.0";

        return version.Split('+')[0];
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var chars = new List<char>();

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (char.IsUpper(character) && index > 0)
            {
                chars.Add('-');
            }

            chars.Add(char.ToLowerInvariant(character));
        }

        return new string(chars.ToArray());
    }
}
