using System.Text.Json;
using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests.Services;

public sealed class CaptureRuntimeResultReaderTests
{
    [Fact]
    public async Task ReadAsync_実行結果JSONを読み込める()
    {
        // Arrange
        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            tempDirectory);

        var resultFilePath =
            Path.Combine(
                tempDirectory,
                "capture-result.json");

        try
        {
            var expected =
                new CaptureRuntimeResult(
                    Succeeded: true,
                    FilePath: "/var/lib/linux-edge-inspection-runtime/captures/capture.jpg",
                    CompletedAt:
                        new DateTimeOffset(
                            2026,
                            8,
                            15,
                            7,
                            25,
                            0,
                            TimeSpan.Zero),
                    ErrorCode: null,
                    ErrorMessage: null);

            var json =
                JsonSerializer.Serialize(
                    expected);

            await File.WriteAllTextAsync(
                resultFilePath,
                json);

            var reader =
                new CaptureRuntimeResultReader(
                    resultFilePath);

            // Act
            var actual =
                await reader.ReadAsync();

            // Assert
            Assert.Equal(
                expected.Succeeded,
                actual.Succeeded);

            Assert.Equal(
                expected.FilePath,
                actual.FilePath);

            Assert.Equal(
                expected.CompletedAt,
                actual.CompletedAt);

            Assert.Equal(
                expected.ErrorCode,
                actual.ErrorCode);

            Assert.Equal(
                expected.ErrorMessage,
                actual.ErrorMessage);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true);
            }
        }

    }

    [Fact]
    public async Task ReadAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            tempDirectory);

        var resultFilePath =
            Path.Combine(
                tempDirectory,
                "capture-result.json");

        try
        {
            var reader =
                new CaptureRuntimeResultReader(
                    resultFilePath);

            await Assert.ThrowsAsync<FileNotFoundException>(
                () => reader.ReadAsync());
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true);
            }
        }
    }

    [Fact]
    public async Task ReadAsync_WhenJsonIsInvalid_ThrowsJsonException()
    {
        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            tempDirectory);

        var resultFilePath =
            Path.Combine(
                tempDirectory,
                "capture-result.json");

        try
        {
            await File.WriteAllTextAsync(
                resultFilePath,
                "{ invalid json }");

            var reader =
                new CaptureRuntimeResultReader(
                    resultFilePath);

            await Assert.ThrowsAsync<JsonException>(
                () => reader.ReadAsync());
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(
                    tempDirectory,
                    recursive: true);
            }
        }
    }
}