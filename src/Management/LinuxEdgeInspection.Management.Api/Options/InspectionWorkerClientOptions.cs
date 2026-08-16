namespace LinuxEdgeInspection.Management.Api.Options;

public sealed class InspectionWorkerClientOptions
{
    public const string SectionName = "InspectionWorkerClient";

    public string SocketPath { get; set; } =
        "/run/linux-edge-inspection/inspection-worker.sock";

    public int TimeoutSeconds { get; set; } = 60;
}
