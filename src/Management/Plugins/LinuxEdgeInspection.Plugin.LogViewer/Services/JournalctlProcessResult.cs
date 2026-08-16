namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public sealed record JournalctlProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
