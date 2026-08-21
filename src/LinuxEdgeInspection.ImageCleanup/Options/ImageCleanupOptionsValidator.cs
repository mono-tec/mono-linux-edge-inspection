using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.ImageCleanup.Options;

/// <summary>
/// <see cref="ImageCleanupOptions"/>を検証します。
/// </summary>
public sealed class ImageCleanupOptionsValidator
    : IValidateOptions<ImageCleanupOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        ImageCleanupOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.RootDirectory))
        {
            failures.Add(
                "ImageCleanup:RootDirectoryは必須です。");
        }
        else
        {
            ValidateRootDirectory(
                options.RootDirectory,
                failures);
        }

        if (options.RetentionDays < 1)
        {
            failures.Add(
                "ImageCleanup:RetentionDaysは1以上で指定してください。");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateRootDirectory(
        string rootDirectory,
        ICollection<string> failures)
    {
        var isAbsolutePath =
            Path.IsPathFullyQualified(rootDirectory) ||
            rootDirectory.StartsWith(
                "/",
                StringComparison.Ordinal);

        if (!isAbsolutePath)
        {
            failures.Add(
                "ImageCleanup:RootDirectoryは絶対パスで指定してください。");

            return;
        }

        try
        {
            var fullPath = Path.GetFullPath(rootDirectory);
            var pathRoot = Path.GetPathRoot(fullPath);

            if (string.Equals(
                    fullPath,
                    pathRoot,
                    OperatingSystem.IsWindows()
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal))
            {
                failures.Add(
                    "ImageCleanup:RootDirectoryにファイルシステムのルートは指定できません。");
            }
        }
        catch (Exception exception)
            when (exception is ArgumentException or
                NotSupportedException or
                PathTooLongException)
        {
            failures.Add(
                $"ImageCleanup:RootDirectoryが不正です。{exception.Message}");
        }
    }
}
