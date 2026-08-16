using LinuxEdgeInspection.Plugin.LogViewer.Models;

namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public static class JournalApplicationCatalog
{
    private static readonly IReadOnlyDictionary<LogApplication, JournalApplication>
        Applications = new Dictionary<LogApplication, JournalApplication>
        {
            [LogApplication.Management] = new(
                "Management",
                "linux-edge-inspection-management.service"),
            [LogApplication.ManagementApi] = new(
                "Management.Api",
                "linux-edge-inspection-management-api.service"),
            [LogApplication.InspectionWorker] = new(
                "InspectionWorker",
                "linux-edge-inspection-inspection-worker.service"),
            [LogApplication.CaptureRequestListener] = new(
                "CaptureRequestListener",
                "linux-edge-inspection-capture-request-listener.service"),
            [LogApplication.Runtime] = new(
                "Runtime",
                "linux-edge-inspection-runtime.service")
        };

    public static JournalApplication Get(LogApplication application) =>
        Applications.TryGetValue(application, out var value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(application));

    public static IReadOnlyCollection<LogApplication> SupportedApplications =>
        Applications.Keys.ToArray();
}

public sealed record JournalApplication(
    string Component,
    string SystemdUnit);
