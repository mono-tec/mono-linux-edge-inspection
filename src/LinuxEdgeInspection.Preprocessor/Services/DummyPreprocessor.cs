using LinuxEdgeInspection.Contracts.Preprocessing;

namespace LinuxEdgeInspection.Preprocessor.Services;

/// <summary>
/// 入力画像を加工せず、そのまま後続処理へ渡します。
/// </summary>
public sealed class DummyPreprocessor
    : IPreprocessor
{
    public Task<PreprocessResult> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
        {
            return Task.FromResult(
                new PreprocessResult(
                    Succeeded: false,
                    FilePaths: Array.Empty<string>(),
                    ErrorCode: PreprocessErrorCodes.InputNotFound,
                    ErrorMessage:
                        $"Preprocess input file was not found: {filePath}"));
        }

        return Task.FromResult(
            new PreprocessResult(
                Succeeded: true,
                FilePaths: new[] { filePath },
                ErrorCode: null,
                ErrorMessage: null));
    }
}
