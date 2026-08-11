namespace LinuxEdgeInspection.CaptureRequestListener.Options;

/// <summary>
/// 撮影Runtimeのsystemd起動設定です。
/// </summary>
public sealed class CaptureRuntimeLauncherOptions
{
    /// <summary>
    /// systemctlコマンドのパスです。
    /// </summary>
    public string SystemctlPath { get; set; } =
        "/usr/bin/systemctl";

    /// <summary>
    /// 撮影Runtimeのsystemd Unit名です。
    /// </summary>
    public string ServiceName { get; set; } =
        "kakip-edge-platform-runtime.service";

    /// <summary>
    /// 起動処理のタイムアウト秒数です。
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}