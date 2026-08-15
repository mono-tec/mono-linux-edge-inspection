using System.Text.Json;
using LinuxEdgeInspection.Runtime.Models;

namespace LinuxEdgeInspection.Runtime.Services;

/// <summary>
/// 撮影Runtimeの実行結果をJSONファイルへ保存します。
/// </summary>
public sealed class RuntimeCaptureResultWriter
    : IRuntimeCaptureResultWriter
{
    private readonly string _resultFilePath;

    /// <summary>
    /// <see cref="RuntimeCaptureResultWriter"/>を初期化します。
    /// </summary>
    /// <param name="resultFilePath">
    /// 実行結果JSONの保存先パスです。
    /// </param>
    public RuntimeCaptureResultWriter(
        string resultFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resultFilePath);

        _resultFilePath = resultFilePath;
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        RuntimeCaptureResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        var directoryPath =
            Path.GetDirectoryName(_resultFilePath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json =
            JsonSerializer.Serialize(
                result,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            _resultFilePath,
            json,
            cancellationToken);
    }
}