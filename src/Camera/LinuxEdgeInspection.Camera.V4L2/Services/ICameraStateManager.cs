using LinuxEdgeInspection.Camera.V4L2.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// カメラ機能の内部状態を管理する機能を定義します。
/// </summary>
public interface ICameraStateManager
{
    /// <summary>
    /// 現在の状態を取得します。
    /// </summary>
    CameraState CurrentState { get; }

    /// <summary>
    /// 指定した状態へ遷移します。
    /// </summary>
    /// <param name="nextState">
    /// 遷移先の状態です。
    /// </param>
    void TransitionTo(CameraState nextState);

    /// <summary>
    /// 指定した状態へ遷移可能か確認します。
    /// </summary>
    /// <param name="nextState">
    /// 遷移先の状態です。
    /// </param>
    /// <returns>
    /// 遷移可能な場合は<c>true</c>。
    /// </returns>
    bool CanTransitionTo(CameraState nextState);
}