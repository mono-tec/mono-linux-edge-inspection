namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// カメラ画像ファイルの保存先管理と検証処理を定義します。
/// </summary>
public interface ICameraFileService
{
    /// <summary>
    /// 保存先ディレクトリが存在することを確認し、
    /// 存在しない場合は作成します。
    /// </summary>
    /// <param name="outputDirectory">
    /// 画像を保存するディレクトリです。
    /// </param>
    void EnsureOutputDirectory(string outputDirectory);

    /// <summary>
    /// 撮影日時を基に正式な画像ファイルパスを生成します。
    /// </summary>
    /// <param name="outputDirectory">
    /// 画像を保存するディレクトリです。
    /// </param>
    /// <param name="capturedAt">
    /// ファイル名へ使用する撮影日時です。
    /// </param>
    /// <returns>
    /// 生成された画像ファイルの絶対パスまたは結合済みパスです。
    /// </returns>
    string CreateOutputPath(
        string outputDirectory,
        DateTimeOffset capturedAt);

    /// <summary>
    /// 正式な画像ファイルパスから一時ファイルパスを生成します。
    /// </summary>
    /// <param name="outputPath">
    /// 正式な画像ファイルパスです。
    /// </param>
    /// <returns>
    /// 撮影処理中に使用する一時ファイルパスです。
    /// </returns>
    string CreateTemporaryPath(string outputPath);

    /// <summary>
    /// 撮影後の一時ファイルが有効か確認します。
    /// </summary>
    /// <param name="temporaryPath">
    /// 検証する一時ファイルのパスです。
    /// </param>
    /// <returns>
    /// ファイルが存在し、サイズが0より大きい場合は<c>true</c>。
    /// </returns>
    bool IsValidCaptureFile(string temporaryPath);

    /// <summary>
    /// 一時ファイルを正式な画像ファイルへ移動します。
    /// </summary>
    /// <param name="temporaryPath">
    /// 一時ファイルのパスです。
    /// </param>
    /// <param name="outputPath">
    /// 正式な画像ファイルのパスです。
    /// </param>
    void MoveToOutput(
        string temporaryPath,
        string outputPath);

    /// <summary>
    /// 指定された一時ファイルを、存在する場合に削除します。
    /// </summary>
    /// <param name="temporaryPath">
    /// 削除対象の一時ファイルパスです。
    /// </param>
    void DeleteTemporaryFile(string temporaryPath);

    /// <summary>
    /// 指定されたファイルのサイズを取得します。
    /// </summary>
    /// <param name="filePath">
    /// サイズを取得するファイルパスです。
    /// </param>
    /// <returns>
    /// ファイルサイズをバイト単位で返します。
    /// </returns>
    long GetFileSize(string filePath);
}