using LinuxEdgeInspection.Camera.V4L2.Services;

namespace LinuxEdgeInspection.Camera.V4L2.Tests;

public sealed class CameraFileServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly CameraFileService _service = new();

    public CameraFileServiceTests()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "KakipEdgePlatform.Tests",
            Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void EnsureOutputDirectory_WhenDirectoryDoesNotExist_CreatesDirectory()
    {
        _service.EnsureOutputDirectory(_testDirectory);

        Assert.True(Directory.Exists(_testDirectory));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void EnsureOutputDirectory_WhenPathIsEmpty_ThrowsArgumentException(
        string outputDirectory)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => _service.EnsureOutputDirectory(outputDirectory));

        Assert.Equal("outputDirectory", exception.ParamName);
    }

    [Fact]
    public void CreateOutputPath_ReturnsExpectedFileName()
    {
        var capturedAt = new DateTimeOffset(
            2026,
            8,
            5,
            19,
            59,
            30,
            123,
            TimeSpan.FromHours(9));

        var outputPath = _service.CreateOutputPath(
            _testDirectory,
            capturedAt);

        var expectedPath = Path.Combine(
            _testDirectory,
            "capture_20260805_195930_123.jpg");

        Assert.Equal(expectedPath, outputPath);
    }

    [Fact]
    public void CreateTemporaryPath_AppendsTemporaryExtension()
    {
        var outputPath = Path.Combine(
            _testDirectory,
            "capture.jpg");

        var temporaryPath = _service.CreateTemporaryPath(outputPath);

        Assert.Equal(outputPath + ".tmp", temporaryPath);
    }

    [Fact]
    public void IsValidCaptureFile_WhenFileExistsAndHasContent_ReturnsTrue()
    {
        Directory.CreateDirectory(_testDirectory);

        var temporaryPath = Path.Combine(
            _testDirectory,
            "capture.jpg.tmp");

        File.WriteAllBytes(
            temporaryPath,
            [0x01, 0x02, 0x03]);

        var result = _service.IsValidCaptureFile(temporaryPath);

        Assert.True(result);
    }

    [Fact]
    public void IsValidCaptureFile_WhenFileDoesNotExist_ReturnsFalse()
    {
        var temporaryPath = Path.Combine(
            _testDirectory,
            "not-found.jpg.tmp");

        var result = _service.IsValidCaptureFile(temporaryPath);

        Assert.False(result);
    }

    [Fact]
    public void IsValidCaptureFile_WhenFileSizeIsZero_ReturnsFalse()
    {
        Directory.CreateDirectory(_testDirectory);

        var temporaryPath = Path.Combine(
            _testDirectory,
            "empty.jpg.tmp");

        File.WriteAllBytes(
            temporaryPath,
            []);

        var result = _service.IsValidCaptureFile(temporaryPath);

        Assert.False(result);
    }

    [Fact]
    public void MoveToOutput_WhenTemporaryFileExists_MovesFile()
    {
        Directory.CreateDirectory(_testDirectory);

        var temporaryPath = Path.Combine(
            _testDirectory,
            "capture.jpg.tmp");

        var outputPath = Path.Combine(
            _testDirectory,
            "capture.jpg");

        File.WriteAllBytes(
            temporaryPath,
            [0x01, 0x02, 0x03]);

        _service.MoveToOutput(
            temporaryPath,
            outputPath);

        Assert.False(File.Exists(temporaryPath));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void MoveToOutput_WhenTemporaryFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var temporaryPath = Path.Combine(
            _testDirectory,
            "not-found.jpg.tmp");

        var outputPath = Path.Combine(
            _testDirectory,
            "capture.jpg");

        Assert.Throws<FileNotFoundException>(
            () => _service.MoveToOutput(
                temporaryPath,
                outputPath));
    }

    [Fact]
    public void MoveToOutput_WhenOutputFileAlreadyExists_ThrowsIOException()
    {
        Directory.CreateDirectory(_testDirectory);

        var temporaryPath = Path.Combine(
            _testDirectory,
            "capture.jpg.tmp");

        var outputPath = Path.Combine(
            _testDirectory,
            "capture.jpg");

        File.WriteAllBytes(
            temporaryPath,
            [0x01]);

        File.WriteAllBytes(
            outputPath,
            [0x02]);

        Assert.Throws<IOException>(
            () => _service.MoveToOutput(
                temporaryPath,
                outputPath));
    }

    [Fact]
    public void DeleteTemporaryFile_WhenFileExists_DeletesFile()
    {
        Directory.CreateDirectory(_testDirectory);

        var temporaryPath = Path.Combine(
            _testDirectory,
            "capture.jpg.tmp");

        File.WriteAllBytes(
            temporaryPath,
            [0x01]);

        _service.DeleteTemporaryFile(temporaryPath);

        Assert.False(File.Exists(temporaryPath));
    }

    [Fact]
    public void DeleteTemporaryFile_WhenFileDoesNotExist_DoesNotThrow()
    {
        var temporaryPath = Path.Combine(
            _testDirectory,
            "not-found.jpg.tmp");

        var exception = Record.Exception(
            () => _service.DeleteTemporaryFile(temporaryPath));

        Assert.Null(exception);
    }

    [Fact]
    public void GetFileSize_WhenFileExists_ReturnsFileSize()
    {
        Directory.CreateDirectory(_testDirectory);

        var filePath = Path.Combine(
            _testDirectory,
            "capture.jpg");

        File.WriteAllBytes(
            filePath,
            [0x01, 0x02, 0x03, 0x04]);

        var fileSize = _service.GetFileSize(filePath);

        Assert.Equal(4, fileSize);
    }

    [Fact]
    public void GetFileSize_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var filePath = Path.Combine(
            _testDirectory,
            "not-found.jpg");

        Assert.Throws<FileNotFoundException>(
            () => _service.GetFileSize(filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(
                _testDirectory,
                recursive: true);
        }
    }
}