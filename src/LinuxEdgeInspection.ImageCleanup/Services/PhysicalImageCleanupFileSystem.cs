namespace LinuxEdgeInspection.ImageCleanup.Services;

/// <summary>
/// 実Filesystemを使用するImageCleanup操作です。
/// </summary>
public sealed class PhysicalImageCleanupFileSystem
    : IImageCleanupFileSystem
{
    public bool DirectoryExists(string path) =>
        Directory.Exists(path);

    public IEnumerable<string> EnumerateFiles(string path) =>
        Directory.EnumerateFiles(
            path,
            "*",
            SearchOption.TopDirectoryOnly);

    public bool IsSymbolicLink(
        string path,
        bool directory)
    {
        FileSystemInfo info = directory
            ? new DirectoryInfo(path)
            : new FileInfo(path);

        return info.LinkTarget is not null;
    }

    public DateTimeOffset GetLastWriteTimeUtc(string path) =>
        File.GetLastWriteTimeUtc(path);

    public void DeleteFile(string path) =>
        File.Delete(path);
}
