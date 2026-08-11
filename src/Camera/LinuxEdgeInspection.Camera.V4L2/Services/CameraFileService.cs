namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// カメラ画像ファイルの保存先管理と検証処理を提供します。
/// </summary>
public sealed class CameraFileService : ICameraFileService
{
    private const string FilePrefix = "capture";
    private const string FileExtension = ".jpg";
    private const string TemporaryExtension = ".tmp";

    /// <inheritdoc />
    public void EnsureOutputDirectory(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException(
                "画像の保存先ディレクトリを指定してください。",
                nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);
    }

    /// <inheritdoc />
    public string CreateOutputPath(
        string outputDirectory,
        DateTimeOffset capturedAt)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException(
                "画像の保存先ディレクトリを指定してください。",
                nameof(outputDirectory));
        }

        var fileName =
            $"{FilePrefix}_{capturedAt:yyyyMMdd_HHmmss_fff}{FileExtension}";

        return Path.Combine(outputDirectory, fileName);
    }

    /// <inheritdoc />
    public string CreateTemporaryPath(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "正式な画像ファイルパスを指定してください。",
                nameof(outputPath));
        }

        return outputPath + TemporaryExtension;
    }

    /// <inheritdoc />
    public bool IsValidCaptureFile(string temporaryPath)
    {
        if (string.IsNullOrWhiteSpace(temporaryPath))
        {
            return false;
        }

        var fileInfo = new FileInfo(temporaryPath);

        return fileInfo.Exists && fileInfo.Length > 0;
    }

    /// <inheritdoc />
    public void MoveToOutput(
        string temporaryPath,
        string outputPath)
    {
        if (string.IsNullOrWhiteSpace(temporaryPath))
        {
            throw new ArgumentException(
                "一時ファイルパスを指定してください。",
                nameof(temporaryPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "正式な画像ファイルパスを指定してください。",
                nameof(outputPath));
        }

        if (!File.Exists(temporaryPath))
        {
            throw new FileNotFoundException(
                "一時ファイルが見つかりません。",
                temporaryPath);
        }

        if (File.Exists(outputPath))
        {
            throw new IOException(
                $"出力先ファイルはすでに存在します。Path: {outputPath}");
        }

        File.Move(temporaryPath, outputPath);
    }

    /// <inheritdoc />
    public void DeleteTemporaryFile(string temporaryPath)
    {
        if (string.IsNullOrWhiteSpace(temporaryPath))
        {
            return;
        }

        if (File.Exists(temporaryPath))
        {
            File.Delete(temporaryPath);
        }
    }

    /// <inheritdoc />
    public long GetFileSize(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "ファイルパスを指定してください。",
                nameof(filePath));
        }

        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException(
                "ファイルが見つかりません。",
                filePath);
        }

        return fileInfo.Length;
    }
}