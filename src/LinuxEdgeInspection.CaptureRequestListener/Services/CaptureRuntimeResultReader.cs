using System.Text.Json;
using LinuxEdgeInspection.CaptureRequestListener.Models;

namespace LinuxEdgeInspection.CaptureRequestListener.Services;

/// <summary>
/// 撮影Runtimeの実行結果をJSONファイルから読み込みます。
/// </summary>
public sealed class CaptureRuntimeResultReader
    : ICaptureRuntimeResultReader
{
    private readonly string _resultFilePath;

    /// <summary>
    /// <see cref="CaptureRuntimeResultReader"/>を初期化します。
    /// </summary>
    /// <param name="resultFilePath">
    /// 撮影Runtimeの実行結果JSONファイルのパスです。
    /// </param>
    public CaptureRuntimeResultReader(
        string resultFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resultFilePath);

        _resultFilePath = resultFilePath;
    }

    /// <inheritdoc />
    public async Task<CaptureRuntimeResult> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        var json =
            await File.ReadAllTextAsync(
                _resultFilePath,
                cancellationToken);

        return JsonSerializer.Deserialize<CaptureRuntimeResult>(
            json)
            ?? throw new InvalidOperationException(
                "撮影Runtimeの実行結果を読み込めませんでした。");
    }
}