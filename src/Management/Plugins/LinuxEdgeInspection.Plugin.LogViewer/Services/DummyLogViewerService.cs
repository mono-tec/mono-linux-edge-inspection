using LinuxEdgeInspection.Plugin.LogViewer.Models;

namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

/// <summary>
/// Linux実ログ連携前に画面動作を確認するためのダミー実装です。
/// </summary>
public sealed class DummyLogViewerService : ILogViewerService
{
    public Task<IReadOnlyList<LogEntry>> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.Now;

        IReadOnlyList<LogEntry> entries =
        [
            new(now.AddSeconds(-12), "Information", "Management", "Management host is ready."),
            new(now.AddSeconds(-8), "Information", "Camera", "Camera diagnostic service is available."),
            new(now.AddSeconds(-4), "Warning", "Storage", "Dummy storage threshold check completed."),
            new(now, "Information", "Inspection", "Waiting for the next inspection request.")
        ];

        return Task.FromResult(entries);
    }
}
