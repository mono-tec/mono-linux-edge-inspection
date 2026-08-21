using LinuxEdgeInspection.ImageCleanup.Models;

namespace LinuxEdgeInspection.ImageCleanup.Services;

/// <summary>
/// 撮像画像をCleanupします。
/// </summary>
public interface IImageCleanupService
{
    ImageCleanupResult Cleanup(
        CancellationToken cancellationToken = default);
}
