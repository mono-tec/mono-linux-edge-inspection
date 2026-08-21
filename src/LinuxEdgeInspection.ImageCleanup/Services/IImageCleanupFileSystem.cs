namespace LinuxEdgeInspection.ImageCleanup.Services;

/// <summary>
/// ImageCleanupで使用するFilesystem操作です。
/// </summary>
public interface IImageCleanupFileSystem
{
    bool DirectoryExists(string path);

    IEnumerable<string> EnumerateFiles(string path);

    bool IsSymbolicLink(string path, bool directory);

    DateTimeOffset GetLastWriteTimeUtc(string path);

    void DeleteFile(string path);
}
