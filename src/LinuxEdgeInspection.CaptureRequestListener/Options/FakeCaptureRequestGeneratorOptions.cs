namespace LinuxEdgeInspection.CaptureRequestListener.Options;

/// <summary>
/// Fake PLC撮影要求Generatorの設定です。
/// </summary>
public sealed class FakeCaptureRequestGeneratorOptions
{
    /// <summary>
    /// Fake撮影要求の自動生成を有効にするかどうかです。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 最初の要求を生成するまでの待機秒数です。
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 1;

    /// <summary>
    /// 各要求を生成する間隔の秒数です。
    /// </summary>
    public int IntervalSeconds { get; set; } = 1;

    /// <summary>
    /// 最初に生成する要求番号です。
    /// </summary>
    public long StartRequestId { get; set; } = 101;

    /// <summary>
    /// 生成する要求件数です。
    /// </summary>
    public int RequestCount { get; set; } = 3;
}