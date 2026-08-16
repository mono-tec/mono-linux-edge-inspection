using LinuxEdgeInspection.Management.Abstractions;

namespace LinuxEdgeInspection.Management.Components.Icons;

public static class PluginIconRenderer
{
    public const string DashboardSvg = """
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>
        """;

    private const string PluginSvg = """
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M8 3v4M16 3v4M5 10h14M7 7h10a2 2 0 0 1 2 2v9a3 3 0 0 1-3 3H8a3 3 0 0 1-3-3V9a2 2 0 0 1 2-2Z"/></svg>
        """;

    private const string DiskSvg = """
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect x="3" y="4" width="18" height="7" rx="2"/><rect x="3" y="13" width="18" height="7" rx="2"/><path d="M7 8h.01M7 17h.01M11 8h7M11 17h7"/></svg>
        """;

    private const string CameraSvg = """
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M4 7h4l2-2h4l2 2h4a1 1 0 0 1 1 1v11H3V8a1 1 0 0 1 1-1Z"/><circle cx="12" cy="13" r="4"/></svg>
        """;

    private const string LogSvg = """
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M5 3h14v18H5zM8 8h8M8 12h8M8 16h5"/></svg>
        """;

    public static string GetSvg(PluginIcon icon) => icon switch
    {
        PluginIcon.DiskMonitor => DiskSvg,
        PluginIcon.CameraTest => CameraSvg,
        PluginIcon.LogViewer => LogSvg,
        _ => PluginSvg
    };
}
