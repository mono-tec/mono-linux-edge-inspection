using LinuxEdgeInspection.Contracts.Preprocessing;

namespace LinuxEdgeInspection.Preprocessor.Services;

public interface IPreprocessor
{
    Task<PreprocessResult> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
