namespace LinuxEdgeInspection.ImageCleanup.Models;

/// <summary>
/// 画像Cleanupの実行結果です。
/// </summary>
public sealed record ImageCleanupResult(
    int TargetCount,
    int DeletedCount,
    int SkippedCount,
    int FailedCount);
