namespace LinuxEdgeInspection.ImageCleanup.Options;

/// <summary>
/// 撮像画像のCleanup設定です。
/// </summary>
public sealed class ImageCleanupOptions
{
    public const string SectionName = "ImageCleanup";

    public string RootDirectory { get; set; } =
        "/var/lib/linux-edge-inspection/captures";

    public int RetentionDays { get; set; } = 7;

    public bool DryRun { get; set; }
}
