namespace LinuxEdgeInspection.InspectionWorker.Options;

public sealed class CaptureRequestClientOptions
{
    public const string SectionName = "CaptureRequestClient";

    public string SocketPath { get; set; } =
        "/run/linux-edge-inspection/capture-request-listener.sock";

    public int TimeoutSeconds { get; set; } = 30;
}
