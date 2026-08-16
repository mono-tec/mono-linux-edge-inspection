namespace LinuxEdgeInspection.Management.Api.Services;

public interface ICaptureImageFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    bool IsSymbolicLink(string path, bool directory);

    Stream OpenRead(string path);
}
