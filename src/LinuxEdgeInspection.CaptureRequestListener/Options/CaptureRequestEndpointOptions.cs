namespace LinuxEdgeInspection.CaptureRequestListener.Options;

public sealed class CaptureRequestEndpointOptions
{
    public const string SectionName = "CaptureRequestEndpoint";

    public string SocketPath { get; set; } =
        "/run/linux-edge-inspection/capture-request-listener.sock";

    public int Backlog { get; set; } = 16;
}
