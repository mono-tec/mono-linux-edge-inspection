using LinuxEdgeInspection.Management.Api.Options;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.Management.Api.Services;

public sealed class CaptureImageService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" };

    private readonly string _rootDirectory;
    private readonly string _rootPrefix;
    private readonly ICaptureImageFileSystem _fileSystem;

    public CaptureImageService(
        IOptions<CaptureImageOptions> options,
        ICaptureImageFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(nameof(fileSystem));

        ArgumentException.ThrowIfNullOrWhiteSpace(
            options.Value.RootDirectory);
        _rootDirectory = Path.GetFullPath(options.Value.RootDirectory);
        _rootPrefix = Path.EndsInDirectorySeparator(_rootDirectory)
            ? _rootDirectory
            : _rootDirectory + Path.DirectorySeparatorChar;
    }

    public CaptureImageOpenResult Open(string fileName)
    {
        if (!IsValidFileName(fileName))
        {
            return new(CaptureImageOpenStatus.InvalidFileName);
        }

        var candidate = Path.GetFullPath(
            Path.Combine(_rootDirectory, fileName));
        if (!candidate.StartsWith(
                _rootPrefix,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            return new(CaptureImageOpenStatus.InvalidFileName);
        }

        if (!_fileSystem.DirectoryExists(_rootDirectory))
        {
            return new(CaptureImageOpenStatus.NotFound);
        }

        if (HasSymbolicLinkInRootPath() ||
            _fileSystem.IsSymbolicLink(candidate, directory: false))
        {
            return new(CaptureImageOpenStatus.SymbolicLinkRejected);
        }

        if (!_fileSystem.FileExists(candidate))
        {
            return new(CaptureImageOpenStatus.NotFound);
        }

        return new(
            CaptureImageOpenStatus.Success,
            _fileSystem.OpenRead(candidate));
    }

    private static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName is "." or ".." ||
            fileName.Contains('/') ||
            fileName.Contains('\\') ||
            !string.Equals(fileName, Path.GetFileName(fileName),
                StringComparison.Ordinal))
        {
            return false;
        }

        return AllowedExtensions.Contains(Path.GetExtension(fileName));
    }

    private bool HasSymbolicLinkInRootPath()
    {
        var root = Path.GetPathRoot(_rootDirectory);
        if (string.IsNullOrEmpty(root))
        {
            return true;
        }

        var current = root;
        foreach (var segment in _rootDirectory[root.Length..]
                     .Split(Path.DirectorySeparatorChar,
                         StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (_fileSystem.DirectoryExists(current) &&
                _fileSystem.IsSymbolicLink(current, directory: true))
            {
                return true;
            }
        }

        return false;
    }
}
