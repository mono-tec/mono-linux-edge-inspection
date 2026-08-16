namespace LinuxEdgeInspection.Plugin.LogViewer.Models;

/// <summary>
/// Log Viewerで表示対象とするアプリケーション種別を表します。
/// </summary>
public enum LogApplication
{
    /// <summary>
    /// Management UIのログ。
    /// </summary>
    Management,

    /// <summary>
    /// Management APIのログ。
    /// </summary>
    ManagementApi,

    /// <summary>
    /// 検査パイプラインを制御するInspectionWorkerのログ。
    /// </summary>
    InspectionWorker,

    /// <summary>
    /// 撮像要求を受け付けてRuntimeへ処理を渡すCaptureRequestListenerのログ。
    /// </summary>
    CaptureRequestListener,

    /// <summary>
    /// カメラ撮像などの実処理を行うRuntimeのログ。
    /// </summary>
    Runtime
}