namespace LinuxEdgeInspection.Management.Api.Options;

public sealed class CaptureImageOptions
{
    public const string SectionName = "CaptureImages";

    public string RootDirectory { get; set; } =
        "/var/lib/linux-edge-inspection-runtime/captures";
}
