namespace LinuxEdgeInspection.Plugin.LogViewer.Models;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Component,
    string Message);
