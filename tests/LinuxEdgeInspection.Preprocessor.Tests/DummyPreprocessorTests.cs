using LinuxEdgeInspection.Contracts.Preprocessing;
using LinuxEdgeInspection.Preprocessor.Services;

namespace LinuxEdgeInspection.Preprocessor.Tests;

public sealed class DummyPreprocessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenInputExists_ReturnsInputFilePath()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            var preprocessor = new DummyPreprocessor();

            var result = await preprocessor.ProcessAsync(filePath);

            Assert.True(result.Succeeded);
            Assert.Equal(new[] { filePath }, result.FilePaths);
            Assert.Null(result.ErrorCode);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void PreprocessResult_CanContainMultipleFilePaths()
    {
        var result = new PreprocessResult(
            Succeeded: true,
            FilePaths: new[] { "roi-1.jpg", "roi-2.jpg" },
            ErrorCode: null,
            ErrorMessage: null);

        Assert.Equal(2, result.FilePaths.Count);
    }

    [Fact]
    public async Task ProcessAsync_WhenInputDoesNotExist_ReturnsInputNotFound()
    {
        var preprocessor = new DummyPreprocessor();
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.jpg");

        var result = await preprocessor.ProcessAsync(filePath);

        Assert.False(result.Succeeded);
        Assert.Empty(result.FilePaths);
        Assert.Equal(
            PreprocessErrorCodes.InputNotFound,
            result.ErrorCode);
    }

    [Fact]
    public async Task ProcessAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var preprocessor = new DummyPreprocessor();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => preprocessor.ProcessAsync("capture.jpg", source.Token));
    }
}
