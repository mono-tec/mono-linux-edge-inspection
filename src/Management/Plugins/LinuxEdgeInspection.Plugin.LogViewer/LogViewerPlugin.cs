using LinuxEdgeInspection.Management.Abstractions;

namespace LinuxEdgeInspection.Plugin.LogViewer;

public sealed class LogViewerPlugin : PluginBase<LogViewerPlugin>
{
    protected override string Name => "Log Viewer";

    protected override string Description => "Displays recent platform log entries.";

    protected override PluginIcon Icon => PluginIcon.LogViewer;
}
