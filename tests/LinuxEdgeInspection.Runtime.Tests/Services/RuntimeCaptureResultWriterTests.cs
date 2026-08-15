using System.Text.Json;
using LinuxEdgeInspection.Runtime.Models;
using LinuxEdgeInspection.Runtime.Services;

namespace LinuxEdgeInspection.Runtime.Tests.Services;

public sealed class RuntimeCaptureResultWriterTests
{
    [Fact]
    public async Task WriteAsync_実行結果をJSONファイルへ保存できる()
    {
        // Arrange
        var tempDirectory =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

        var resultFilePath =
            Path.Combine(
                tempDirectory,
                "capture-result.json");

        try
        {
            var writer =
                new RuntimeCaptureResultWriter(
                    resultFilePath);

            var result =
                new RuntimeCaptureResult(
                    Succeeded: true,
                    FilePath: "/tmp/capture.jpg",
                    CompletedAt:
                        new DateTimeOffset(
                            2026,
                            8,
                            15,
                            6,
                            0,
                            0,
                            TimeSpan.Zero),
                    ErrorCode: null,
                    ErrorMessage: null);

            // Act
            await writer.WriteAsync(result);

            // Assert
            Assert.True(
                File.Exists(resultFilePath));

            var json =
                await File.ReadAllTextAsync(
                    resultFilePath);

            var savedResult =
                JsonSerializer.Deserialize<RuntimeCaptureResult>(
                    json);

            Assert.NotNull(savedResult);
            Assert.True(savedResult.Succeeded);
            Assert.Equal(
                "/tmp/capture.jpg",
                savedResult.FilePath);
            Assert.Null(savedResult.ErrorCode);
            Assert.Null(savedResult.ErrorMessage);
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