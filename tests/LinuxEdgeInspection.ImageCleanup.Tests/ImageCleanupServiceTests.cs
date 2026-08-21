using LinuxEdgeInspection.ImageCleanup.Options;
using LinuxEdgeInspection.ImageCleanup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.ImageCleanup.Tests;

public sealed class ImageCleanupServiceTests
{
    private static readonly DateTimeOffset CurrentTime =
        new(
            2026,
            8,
            20,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void Cleanup_保持期間を超えたJpgを削除する()
    {
        using var directory = new TestDirectory();
        var filePath = directory.CreateFile(
            "old.jpg",
            CurrentTime.AddDays(-8));

        var result = CreateService(
            directory.Path).Cleanup();

        Assert.False(File.Exists(filePath));
        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public void Cleanup_保持期間内のJpgを削除しない()
    {
        using var directory = new TestDirectory();
        var filePath = directory.CreateFile(
            "recent.jpg",
            CurrentTime.AddDays(-6));

        var result = CreateService(
            directory.Path).Cleanup();

        Assert.True(File.Exists(filePath));
        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    [Fact]
    public void Cleanup_Cutoffと同時刻のJpgを削除しない()
    {
        using var directory = new TestDirectory();
        var filePath = directory.CreateFile(
            "boundary.jpg",
            CurrentTime.AddDays(-7));

        var result = CreateService(
            directory.Path).Cleanup();

        Assert.True(File.Exists(filePath));
        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    [Theory]
    [InlineData("old.jpeg")]
    [InlineData("old.JPG")]
    [InlineData("old.JPEG")]
    public void Cleanup_Jpegと大文字拡張子を削除する(
        string fileName)
    {
        using var directory = new TestDirectory();
        var filePath = directory.CreateFile(
            fileName,
            CurrentTime.AddDays(-8));

        var result = CreateService(
            directory.Path).Cleanup();

        Assert.False(File.Exists(filePath));
        Assert.Equal(1, result.DeletedCount);
    }

    [Theory]
    [InlineData("image.png")]
    [InlineData("image.gif")]
    [InlineData("image")]
    [InlineData("image.jpg.tmp")]
    public void Cleanup_JpgとJpeg以外を削除しない(
        string fileName)
    {
        using var directory = new TestDirectory();
        var filePath = directory.CreateFile(
            fileName,
            CurrentTime.AddDays(-8));

        var result = CreateService(
            directory.Path).Cleanup();

        Assert.True(File.Exists(filePath));
        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    [Fact]
    public void Cleanup_DryRunでは削除しない()
    {
        using var directory = new TestDirectory();
        var logger = new TestLogger<ImageCleanupService>();
        var filePath = directory.CreateFile(
            "old.jpg",
            CurrentTime.AddDays(-8));

        var result = CreateService(
            directory.Path,
            dryRun: true,
            logger: logger).Cleanup();

        Assert.True(File.Exists(filePath));
        Assert.Equal(1, result.TargetCount);
        Assert.Equal(0, result.DeletedCount);
        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == LogLevel.Information &&
                entry.Message.Contains(
                    "DryRun",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Cleanup_RootDirectoryが存在しなくても失敗しない()
    {
        using var parent = new TestDirectory();
        var missingDirectory = System.IO.Path.Combine(
            parent.Path,
            "missing");
        var logger = new TestLogger<ImageCleanupService>();

        var result = CreateService(
            missingDirectory,
            logger: logger).Cleanup();

        Assert.Equal(0, result.TargetCount);
        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == LogLevel.Warning &&
                entry.Message.Contains(
                    "存在しない",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Cleanup_サブディレクトリ内のファイルを対象にしない()
    {
        using var directory = new TestDirectory();
        var subdirectory = Directory.CreateDirectory(
            System.IO.Path.Combine(
                directory.Path,
                "sub"));
        var filePath = System.IO.Path.Combine(
            subdirectory.FullName,
            "old.jpg");
        File.WriteAllText(filePath, "test");
        File.SetLastWriteTimeUtc(
            filePath,
            CurrentTime.AddDays(-8).UtcDateTime);

        var result = CreateService(
            directory.Path).Cleanup();

        Assert.True(File.Exists(filePath));
        Assert.Equal(0, result.TargetCount);
        Assert.Equal(0, result.DeletedCount);
    }

    [Fact]
    public void Cleanup_削除失敗後も他ファイルの処理を継続する()
    {
        using var directory = new TestDirectory();
        var failedPath = System.IO.Path.Combine(
            directory.Path,
            "01-failed.jpg");
        var successfulPath = System.IO.Path.Combine(
            directory.Path,
            "02-successful.jpg");
        var fileSystem = new FakeImageCleanupFileSystem
        {
            Files = [failedPath, successfulPath],
            DeleteFailurePath = failedPath,
            LastWriteTimeUtc = CurrentTime.AddDays(-8)
        };
        var logger = new TestLogger<ImageCleanupService>();

        var result = CreateService(
            directory.Path,
            fileSystem: fileSystem,
            logger: logger).Cleanup();

        Assert.Equal(2, result.TargetCount);
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains(
            successfulPath,
            fileSystem.DeletedFiles);
        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == LogLevel.Error &&
                entry.Message.Contains(
                    "削除に失敗",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Cleanup_個別ファイルの状態取得失敗後も処理を継続する()
    {
        using var directory = new TestDirectory();
        var failedPath = System.IO.Path.Combine(
            directory.Path,
            "01-state-failed.jpg");
        var successfulPath = System.IO.Path.Combine(
            directory.Path,
            "02-successful.jpg");
        var fileSystem = new FakeImageCleanupFileSystem
        {
            Files = [failedPath, successfulPath],
            StateFailurePath = failedPath,
            LastWriteTimeUtc = CurrentTime.AddDays(-8)
        };
        var logger = new TestLogger<ImageCleanupService>();

        var result = CreateService(
            directory.Path,
            fileSystem: fileSystem,
            logger: logger).Cleanup();

        Assert.Equal(1, result.TargetCount);
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains(
            successfulPath,
            fileSystem.DeletedFiles);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void Cleanup_RootDirectory列挙失敗を結果へ記録する()
    {
        using var directory = new TestDirectory();
        var fileSystem = new FakeImageCleanupFileSystem
        {
            EnumerationException =
                new UnauthorizedAccessException("denied")
        };
        var logger = new TestLogger<ImageCleanupService>();

        var result = CreateService(
            directory.Path,
            fileSystem: fileSystem,
            logger: logger).Cleanup();

        Assert.Equal(1, result.FailedCount);
        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == LogLevel.Error &&
                entry.Message.Contains(
                    "列挙に失敗",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Cleanup_通常のスキップをDebugへ記録する()
    {
        using var directory = new TestDirectory();
        directory.CreateFile(
            "recent.jpg",
            CurrentTime.AddDays(-1));
        directory.CreateFile(
            "old.png",
            CurrentTime.AddDays(-8));
        var logger = new TestLogger<ImageCleanupService>();

        var result = CreateService(
            directory.Path,
            logger: logger).Cleanup();

        Assert.Equal(2, result.SkippedCount);
        Assert.Equal(
            2,
            logger.Entries.Count(
                entry => entry.Level == LogLevel.Debug));
    }

    [Fact]
    public void Cleanup_画像ファイルのSymbolicLink判定時はそのファイルだけスキップする()
    {
        using var directory = new TestDirectory();
        var linkPath = System.IO.Path.Combine(
            directory.Path,
            "linked.jpg");
        var regularPath = System.IO.Path.Combine(
            directory.Path,
            "regular.jpg");
        var fileSystem = new FakeImageCleanupFileSystem
        {
            Files = [linkPath, regularPath],
            SymbolicLinkPaths = [linkPath],
            LastWriteTimeUtc = CurrentTime.AddDays(-8)
        };

        var result = CreateService(
            directory.Path,
            fileSystem: fileSystem).Cleanup();

        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(1, result.DeletedCount);
        Assert.DoesNotContain(
            linkPath,
            fileSystem.DeletedFiles);
        Assert.Contains(
            regularPath,
            fileSystem.DeletedFiles);
    }

    [Fact]
    public void Cleanup_RootDirectoryのSymbolicLink判定時は全体を拒否する()
    {
        using var directory = new TestDirectory();
        var filePath = System.IO.Path.Combine(
            directory.Path,
            "old.jpg");
        var fileSystem = new FakeImageCleanupFileSystem
        {
            Files = [filePath],
            SymbolicLinkPaths = [directory.Path]
        };

        var result = CreateService(
            directory.Path,
            fileSystem: fileSystem).Cleanup();

        Assert.Equal(1, result.FailedCount);
        Assert.Equal(0, result.TargetCount);
        Assert.Empty(fileSystem.DeletedFiles);
    }

    [Fact]
    public void Cleanup_開始対象削除集計終了をInformationへ記録する()
    {
        using var directory = new TestDirectory();
        directory.CreateFile(
            "old.jpg",
            CurrentTime.AddDays(-8));
        var logger = new TestLogger<ImageCleanupService>();

        CreateService(
            directory.Path,
            logger: logger).Cleanup();

        var informationMessages = logger.Entries
            .Where(entry => entry.Level == LogLevel.Information)
            .Select(entry => entry.Message)
            .ToArray();

        Assert.Contains(
            informationMessages,
            message => message.Contains("開始", StringComparison.Ordinal));
        Assert.Contains(
            informationMessages,
            message => message.Contains("RootDirectory", StringComparison.Ordinal));
        Assert.Contains(
            informationMessages,
            message => message.Contains("削除対象", StringComparison.Ordinal));
        Assert.Contains(
            informationMessages,
            message => message.Contains("削除しました", StringComparison.Ordinal));
        Assert.Contains(
            informationMessages,
            message => message.Contains("集計", StringComparison.Ordinal));
        Assert.Contains(
            informationMessages,
            message => message.Contains("終了", StringComparison.Ordinal));
    }

    [Fact]
    public void Cleanup_画像ファイルのSymbolicLinkをスキップする()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        using var directory = new TestDirectory();
        using var outsideDirectory = new TestDirectory();

        // RootDirectory外にリンク先ファイルを作成する
        var outsideFile = outsideDirectory.CreateFile(
            "outside-target.dat",
            CurrentTime.AddDays(-8));

        // RootDirectory直下にはSymbolic Linkだけを配置する
        var linkPath = System.IO.Path.Combine(
            directory.Path,
            "linked.jpg");

        File.CreateSymbolicLink(
            linkPath,
            outsideFile);

        var logger = new TestLogger<ImageCleanupService>();

        var result = CreateService(
            directory.Path,
            logger: logger).Cleanup();

        // リンク先の実ファイルは削除されない
        Assert.True(File.Exists(outsideFile));

        // Symbolic Link自体も残る
        Assert.NotNull(
            new FileInfo(linkPath).LinkTarget);

        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(1, result.SkippedCount);

        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == LogLevel.Warning &&
                entry.Message.Contains(
                    "Symbolic Link",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Cleanup_RootDirectoryがSymbolicLinkの場合は全体を拒否する()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        using var directory = new TestDirectory();
        var targetDirectory = Directory.CreateDirectory(
            System.IO.Path.Combine(
                directory.Path,
                "target"));
        var targetFile = System.IO.Path.Combine(
            targetDirectory.FullName,
            "old.jpg");
        File.WriteAllText(targetFile, "test");
        File.SetLastWriteTimeUtc(
            targetFile,
            CurrentTime.AddDays(-8).UtcDateTime);
        var linkDirectory = System.IO.Path.Combine(
            directory.Path,
            "captures-link");
        Directory.CreateSymbolicLink(
            linkDirectory,
            targetDirectory.FullName);
        var logger = new TestLogger<ImageCleanupService>();

        var result = CreateService(
            linkDirectory,
            logger: logger).Cleanup();

        Assert.True(File.Exists(targetFile));
        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == LogLevel.Error &&
                entry.Message.Contains(
                    "Cleanupを拒否",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void Cleanup_RootDirectoryの親がSymbolicLinkの場合は全体を拒否する()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        using var directory = new TestDirectory();
        var targetParent = Directory.CreateDirectory(
            System.IO.Path.Combine(
                directory.Path,
                "target-parent"));
        var captures = Directory.CreateDirectory(
            System.IO.Path.Combine(
                targetParent.FullName,
                "captures"));
        var targetFile = System.IO.Path.Combine(
            captures.FullName,
            "old.jpg");
        File.WriteAllText(targetFile, "test");
        var linkParent = System.IO.Path.Combine(
            directory.Path,
            "linked-parent");
        Directory.CreateSymbolicLink(
            linkParent,
            targetParent.FullName);
        var rootDirectory = System.IO.Path.Combine(
            linkParent,
            "captures");

        var result = CreateService(
            rootDirectory).Cleanup();

        Assert.True(File.Exists(targetFile));
        Assert.Equal(1, result.FailedCount);
    }

    private static ImageCleanupService CreateService(
        string rootDirectory,
        bool dryRun = false,
        IImageCleanupFileSystem? fileSystem = null,
        ILogger<ImageCleanupService>? logger = null)
    {
        return new ImageCleanupService(
            Microsoft.Extensions.Options.Options.Create(
                new ImageCleanupOptions
                {
                    RootDirectory = rootDirectory,
                    RetentionDays = 7,
                    DryRun = dryRun
                }),
            fileSystem ??
                new PhysicalImageCleanupFileSystem(),
            new FixedTimeProvider(CurrentTime),
            logger ??
                new TestLogger<ImageCleanupService>());
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset currentTime)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            currentTime;
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"linux-edge-inspection-image-cleanup-{Guid.NewGuid():N}");

            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(
            string fileName,
            DateTimeOffset lastWriteTimeUtc)
        {
            var filePath = System.IO.Path.Combine(
                Path,
                fileName);

            File.WriteAllText(
                filePath,
                "test");

            File.SetLastWriteTimeUtc(
                filePath,
                lastWriteTimeUtc.UtcDateTime);

            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(
                    Path,
                    recursive: true);
            }
        }
    }

    private sealed class FakeImageCleanupFileSystem
        : IImageCleanupFileSystem
    {
        public IReadOnlyList<string> Files { get; init; } = [];

        public string? DeleteFailurePath { get; init; }

        public string? StateFailurePath { get; init; }

        public DateTimeOffset LastWriteTimeUtc { get; init; } =
            CurrentTime.AddDays(-8);

        public Exception? EnumerationException { get; init; }

        public HashSet<string> SymbolicLinkPaths { get; init; } = [];

        public List<string> DeletedFiles { get; } = [];

        public bool DirectoryExists(string path) => true;

        public IEnumerable<string> EnumerateFiles(string path)
        {
            if (EnumerationException is not null)
            {
                throw EnumerationException;
            }

            return Files;
        }

        public bool IsSymbolicLink(
            string path,
            bool directory) =>
            SymbolicLinkPaths.Contains(path);

        public DateTimeOffset GetLastWriteTimeUtc(string path)
        {
            if (string.Equals(
                    path,
                    StateFailurePath,
                    StringComparison.Ordinal))
            {
                throw new IOException("state failure");
            }

            return LastWriteTimeUtc;
        }

        public void DeleteFile(string path)
        {
            if (string.Equals(
                    path,
                    DeleteFailurePath,
                    StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(
                    "delete failure");
            }

            DeletedFiles.Add(path);
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(
                new LogEntry(
                    logLevel,
                    formatter(state, exception)));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message);
}
