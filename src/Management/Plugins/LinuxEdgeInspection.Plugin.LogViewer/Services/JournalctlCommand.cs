namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public sealed record JournalctlCommand(
    string ExecutablePath,
    IReadOnlyList<string> Arguments);
