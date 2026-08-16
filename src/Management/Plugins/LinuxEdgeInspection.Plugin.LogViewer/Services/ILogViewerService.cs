using LinuxEdgeInspection.Plugin.LogViewer.Models;

namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public interface ILogViewerService
{
    Task<IReadOnlyList<LogEntry>> GetLogsAsync(CancellationToken cancellationToken = default);
}
