using LinuxEdgeInspection.Plugin.LogViewer.Models;

namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public interface ILogViewerService
{
    Task<LogPage> GetLogsAsync(
        LogQuery query,
        CancellationToken cancellationToken = default);
}
