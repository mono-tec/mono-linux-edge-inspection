namespace LinuxEdgeInspection.InspectionWorker.Options;

public sealed class InspectionRequestEndpointOptions
{
    public const string SectionName = "InspectionRequestEndpoint";

    public string SocketPath { get; set; } =
        "/run/linux-edge-inspection/inspection-worker.sock";

    public int Backlog { get; set; } = 16;
}
