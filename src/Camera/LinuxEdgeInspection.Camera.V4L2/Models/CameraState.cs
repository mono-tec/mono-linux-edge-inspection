namespace LinuxEdgeInspection.Camera.V4L2.Models;

/// <summary>
/// カメラ機能の内部状態を表します。
/// </summary>
public enum CameraState
{
    /// <summary>
    /// カメラ機能が停止している状態です。
    /// </summary>
    Stopped = 0,

    /// <summary>
    /// カメラ機能が利用可能な状態です。
    /// </summary>
    Ready = 1,

    /// <summary>
    /// 撮影処理を実行している状態です。
    /// </summary>
    Capturing = 2,

    /// <summary>
    /// 継続利用が難しい異常が発生した状態です。
    /// </summary>
    Faulted = 3
}