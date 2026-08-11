using LinuxEdgeInspection.Camera.V4L2.Models;
using LinuxEdgeInspection.Camera.V4L2.Services;

namespace LinuxEdgeInspection.Camera.V4L2.Tests;

public sealed class CameraStateManagerTests
{
    [Fact]
    public void Constructor_InitialStateIsStopped()
    {
        var manager = new CameraStateManager();

        Assert.Equal(
            CameraState.Stopped,
            manager.CurrentState);
    }

    [Theory]
    [InlineData(CameraState.Stopped, CameraState.Ready)]
    [InlineData(CameraState.Stopped, CameraState.Faulted)]
    [InlineData(CameraState.Ready, CameraState.Capturing)]
    [InlineData(CameraState.Ready, CameraState.Stopped)]
    [InlineData(CameraState.Ready, CameraState.Faulted)]
    [InlineData(CameraState.Capturing, CameraState.Ready)]
    [InlineData(CameraState.Capturing, CameraState.Stopped)]
    [InlineData(CameraState.Capturing, CameraState.Faulted)]
    [InlineData(CameraState.Faulted, CameraState.Stopped)]
    [InlineData(CameraState.Faulted, CameraState.Ready)]
    public void CanTransitionTo_WhenTransitionIsAllowed_ReturnsTrue(
        CameraState currentState,
        CameraState nextState)
    {
        var manager = CreateManagerInState(currentState);

        var result = manager.CanTransitionTo(nextState);

        Assert.True(result);
    }

    [Theory]
    [InlineData(CameraState.Stopped, CameraState.Capturing)]
    [InlineData(CameraState.Faulted, CameraState.Capturing)]
    public void CanTransitionTo_WhenTransitionIsNotAllowed_ReturnsFalse(
        CameraState currentState,
        CameraState nextState)
    {
        var manager = CreateManagerInState(currentState);

        var result = manager.CanTransitionTo(nextState);

        Assert.False(result);
    }

    [Theory]
    [InlineData(CameraState.Stopped)]
    [InlineData(CameraState.Ready)]
    [InlineData(CameraState.Capturing)]
    [InlineData(CameraState.Faulted)]
    public void CanTransitionTo_WhenNextStateIsCurrentState_ReturnsTrue(
        CameraState state)
    {
        var manager = CreateManagerInState(state);

        var result = manager.CanTransitionTo(state);

        Assert.True(result);
    }

    [Fact]
    public void TransitionTo_WhenTransitionIsAllowed_ChangesCurrentState()
    {
        var manager = new CameraStateManager();

        manager.TransitionTo(CameraState.Ready);

        Assert.Equal(
            CameraState.Ready,
            manager.CurrentState);
    }

    [Fact]
    public void TransitionTo_WhenTransitionIsNotAllowed_ThrowsInvalidOperationException()
    {
        var manager = new CameraStateManager();

        var exception = Assert.Throws<InvalidOperationException>(
            () => manager.TransitionTo(CameraState.Capturing));

        Assert.Equal(
            CameraState.Stopped,
            manager.CurrentState);

        Assert.Contains(
            "Stopped",
            exception.Message);

        Assert.Contains(
            "Capturing",
            exception.Message);
    }

    [Fact]
    public void TransitionTo_WhenNextStateIsCurrentState_DoesNotThrow()
    {
        var manager = new CameraStateManager();

        var exception = Record.Exception(
            () => manager.TransitionTo(CameraState.Stopped));

        Assert.Null(exception);

        Assert.Equal(
            CameraState.Stopped,
            manager.CurrentState);
    }

    [Fact]
    public void TransitionTo_ThroughNormalCaptureFlow_ChangesStatesInOrder()
    {
        var manager = new CameraStateManager();

        manager.TransitionTo(CameraState.Ready);

        Assert.Equal(
            CameraState.Ready,
            manager.CurrentState);

        manager.TransitionTo(CameraState.Capturing);

        Assert.Equal(
            CameraState.Capturing,
            manager.CurrentState);

        manager.TransitionTo(CameraState.Ready);

        Assert.Equal(
            CameraState.Ready,
            manager.CurrentState);

        manager.TransitionTo(CameraState.Stopped);

        Assert.Equal(
            CameraState.Stopped,
            manager.CurrentState);
    }

    [Fact]
    public void TransitionTo_FromCapturingToFaulted_ChangesCurrentState()
    {
        var manager = CreateManagerInState(
            CameraState.Capturing);

        manager.TransitionTo(CameraState.Faulted);

        Assert.Equal(
            CameraState.Faulted,
            manager.CurrentState);
    }

    private static CameraStateManager CreateManagerInState(
        CameraState state)
    {
        var manager = new CameraStateManager();

        switch (state)
        {
            case CameraState.Stopped:
                break;

            case CameraState.Ready:
                manager.TransitionTo(CameraState.Ready);
                break;

            case CameraState.Capturing:
                manager.TransitionTo(CameraState.Ready);
                manager.TransitionTo(CameraState.Capturing);
                break;

            case CameraState.Faulted:
                manager.TransitionTo(CameraState.Faulted);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state,
                    "未対応のカメラ状態です。");
        }

        return manager;
    }
}