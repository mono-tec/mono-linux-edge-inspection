namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public interface IJournalctlProcessRunner
{
    Task<JournalctlProcessResult> RunAsync(
        JournalctlCommand command,
        CancellationToken cancellationToken = default);
}
