using LinuxEdgeInspection.Camera.V4L2.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// カメラ機能の内部状態と状態遷移を管理します。
/// </summary>
public sealed class CameraStateManager : ICameraStateManager
{
    private readonly object _syncRoot = new();

    private CameraState _currentState = CameraState.Stopped;

    /// <inheritdoc />
    public CameraState CurrentState
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentState;
            }
        }
    }

    /// <inheritdoc />
    public bool CanTransitionTo(CameraState nextState)
    {
        lock (_syncRoot)
        {
            return CanTransition(
                _currentState,
                nextState);
        }
    }

    /// <inheritdoc />
    public void TransitionTo(CameraState nextState)
    {
        lock (_syncRoot)
        {
            if (!CanTransition(
                    _currentState,
                    nextState))
            {
                throw new InvalidOperationException(
                    $"カメラ状態を {_currentState} から {nextState} へ変更できません。");
            }

            _currentState = nextState;
        }
    }

    private static bool CanTransition(
        CameraState currentState,
        CameraState nextState)
    {
        if (currentState == nextState)
        {
            return true;
        }

        return currentState switch
        {
            CameraState.Stopped =>
                nextState is CameraState.Ready
                    or CameraState.Faulted,

            CameraState.Ready =>
                nextState is CameraState.Capturing
                    or CameraState.Stopped
                    or CameraState.Faulted,

            CameraState.Capturing =>
                nextState is CameraState.Ready
                    or CameraState.Stopped
                    or CameraState.Faulted,

            CameraState.Faulted =>
                nextState is CameraState.Stopped
                    or CameraState.Ready,

            _ => false
        };
    }
}