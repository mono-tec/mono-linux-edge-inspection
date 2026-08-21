using LinuxEdgeInspection.ImageCleanup.Models;
using LinuxEdgeInspection.ImageCleanup.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.ImageCleanup.Services;

/// <summary>
/// RootDirectory直下の保持期間を超えた撮像画像を削除します。
/// </summary>
public sealed class ImageCleanupService
    : IImageCleanupService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg"
        };

    private readonly ImageCleanupOptions _options;
    private readonly string _rootDirectory;
    private readonly IImageCleanupFileSystem _fileSystem;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ImageCleanupService> _logger;

    public ImageCleanupService(
        IOptions<ImageCleanupOptions> options,
        IImageCleanupFileSystem fileSystem,
        TimeProvider timeProvider,
        ILogger<ImageCleanupService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _rootDirectory =
            Path.GetFullPath(_options.RootDirectory);

        _fileSystem = fileSystem
            ?? throw new ArgumentNullException(
                nameof(fileSystem));

        _timeProvider = timeProvider
            ?? throw new ArgumentNullException(
                nameof(timeProvider));

        _logger = logger
            ?? throw new ArgumentNullException(
                nameof(logger));
    }

    public ImageCleanupResult Cleanup(
        CancellationToken cancellationToken = default)
    {
        var targetCount = 0;
        var deletedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        _logger.LogInformation(
            "Image Cleanupを開始します。");

        _logger.LogInformation(
            "Image Cleanup設定。RootDirectory: {RootDirectory}, RetentionDays: {RetentionDays}, DryRun: {DryRun}",
            _rootDirectory,
            _options.RetentionDays,
            _options.DryRun);

        try
        {
            try
            {
                if (HasSymbolicLinkInRootPath())
                {
                    failedCount++;

                    _logger.LogError(
                        "RootDirectoryまたはRootDirectoryへ至る既存親要素がSymbolic Linkです。Cleanupを拒否します。RootDirectory: {RootDirectory}",
                        _rootDirectory);

                    return CreateResult();
                }
            }
            catch (Exception exception)
            {
                failedCount++;

                _logger.LogError(
                    exception,
                    "RootDirectoryの安全性を確認できませんでした。Cleanupを拒否します。RootDirectory: {RootDirectory}",
                    _rootDirectory);

                return CreateResult();
            }

            if (!_fileSystem.DirectoryExists(
                    _rootDirectory))
            {
                _logger.LogWarning(
                    "RootDirectoryが存在しないため、Cleanupを終了します。RootDirectory: {RootDirectory}",
                    _rootDirectory);

                return CreateResult();
            }

            var cutoff =
                _timeProvider
                    .GetUtcNow()
                    .AddDays(-_options.RetentionDays);

            try
            {
                foreach (var filePath in
                         _fileSystem.EnumerateFiles(
                             _rootDirectory))
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    ProcessFile(filePath);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failedCount++;

                _logger.LogError(
                    exception,
                    "RootDirectoryの列挙に失敗しました。RootDirectory: {RootDirectory}",
                    _rootDirectory);
            }

            return CreateResult();

            void ProcessFile(string filePath)
            {
                var extension = Path.GetExtension(filePath);

                if (!AllowedExtensions.Contains(extension))
                {
                    skippedCount++;

                    _logger.LogDebug(
                        "拡張子が対象外のためスキップします。FilePath: {FilePath}",
                        filePath);

                    return;
                }

                try
                {
                    if (_fileSystem.IsSymbolicLink(
                            filePath,
                            directory: false))
                    {
                        skippedCount++;

                        _logger.LogWarning(
                            "Symbolic Linkの画像ファイルをスキップします。FilePath: {FilePath}",
                            filePath);

                        return;
                    }
                }
                catch (Exception exception)
                {
                    failedCount++;

                    _logger.LogWarning(
                        exception,
                        "画像ファイルのSymbolic Link状態を取得できないためスキップします。FilePath: {FilePath}",
                        filePath);

                    return;
                }

                DateTimeOffset lastWriteTimeUtc;

                try
                {
                    lastWriteTimeUtc =
                        _fileSystem.GetLastWriteTimeUtc(
                            filePath);
                }
                catch (Exception exception)
                {
                    failedCount++;

                    _logger.LogWarning(
                        exception,
                        "画像ファイルの更新日時を取得できないためスキップします。FilePath: {FilePath}",
                        filePath);

                    return;
                }

                if (lastWriteTimeUtc >= cutoff)
                {
                    skippedCount++;

                    _logger.LogDebug(
                        "保持期間内のためスキップします。FilePath: {FilePath}, LastWriteTimeUtc: {LastWriteTimeUtc}, CutoffUtc: {CutoffUtc}",
                        filePath,
                        lastWriteTimeUtc,
                        cutoff);

                    return;
                }

                targetCount++;

                _logger.LogInformation(
                    "削除対象ファイルです。FilePath: {FilePath}, LastWriteTimeUtc: {LastWriteTimeUtc}, CutoffUtc: {CutoffUtc}",
                    filePath,
                    lastWriteTimeUtc,
                    cutoff);

                if (_options.DryRun)
                {
                    _logger.LogInformation(
                        "DryRunのため削除を実行しません。削除予定ファイル: {FilePath}",
                        filePath);

                    return;
                }

                try
                {
                    _fileSystem.DeleteFile(filePath);
                    deletedCount++;

                    _logger.LogInformation(
                        "画像ファイルを削除しました。FilePath: {FilePath}",
                        filePath);
                }
                catch (Exception exception)
                {
                    failedCount++;

                    _logger.LogError(
                        exception,
                        "画像ファイルの削除に失敗しました。FilePath: {FilePath}",
                        filePath);
                }
            }
        }
        finally
        {
            _logger.LogInformation(
                "Image Cleanup集計。TargetCount: {TargetCount}, DeletedCount: {DeletedCount}, SkippedCount: {SkippedCount}, FailedCount: {FailedCount}",
                targetCount,
                deletedCount,
                skippedCount,
                failedCount);

            _logger.LogInformation(
                "Image Cleanupを終了します。");
        }

        ImageCleanupResult CreateResult() =>
            new(
                targetCount,
                deletedCount,
                skippedCount,
                failedCount);
    }

    private bool HasSymbolicLinkInRootPath()
    {
        var pathRoot =
            Path.GetPathRoot(_rootDirectory)
            ?? throw new InvalidOperationException(
                "RootDirectoryのルートを取得できませんでした。");

        var current = pathRoot;
        var relativePath =
            _rootDirectory[pathRoot.Length..];

        foreach (var segment in relativePath.Split(
                     [
                         Path.DirectorySeparatorChar,
                         Path.AltDirectorySeparatorChar
                     ],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(
                current,
                segment);

            if (_fileSystem.IsSymbolicLink(
                    current,
                    directory: true))
            {
                return true;
            }
        }

        return false;
    }
}
