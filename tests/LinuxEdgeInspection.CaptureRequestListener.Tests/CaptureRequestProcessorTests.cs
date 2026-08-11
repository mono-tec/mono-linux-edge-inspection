using LinuxEdgeInspection.CaptureRequestListener.Models;
using LinuxEdgeInspection.CaptureRequestListener.Services;

namespace LinuxEdgeInspection.CaptureRequestListener.Tests;

/// <summary>
/// <see cref="CaptureRequestProcessor"/> のテストを行います。
/// </summary>
public sealed class CaptureRequestProcessorTests
{
    /// <summary>
    /// Runtimeの起動に成功した場合、
    /// 成功したCaptureResultが返されることを確認します。
    /// </summary>
    [Fact]
    public async Task ProcessAsync_WhenRuntimeSucceeds_ReturnsSuccessfulResult()
    {
        // Arrange
        var completedAt =
            DateTimeOffset.Now;

        var runtimeLauncher =
            new FakeCaptureRuntimeLauncher(
                new CaptureRuntimeLaunchResult(
                    Succeeded: true,
                    ExitCode: 0,
                    StartedAt:
                        completedAt.AddSeconds(-1),
                    CompletedAt: completedAt,
                    ErrorCode: null,
                    ErrorMessage: null));

        var processor =
            new CaptureRequestProcessor(
                runtimeLauncher);

        var request =
            new CaptureRequest(
                RequestId: 101,
                RequestedAt:
                    DateTimeOffset.Now);

        // Act
        var result =
            await processor.ProcessAsync(
                request);

        // Assert
        Assert.Equal(
            1,
            runtimeLauncher.LaunchCount);

        Assert.Equal(
            101,
            result.RequestId);

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            completedAt,
            result.CompletedAt);

        Assert.Null(
            result.ErrorCode);

        Assert.Null(
            result.ErrorMessage);
    }

    /// <summary>
    /// Runtimeの起動に失敗した場合、
    /// 失敗内容を保持したCaptureResultが返されることを確認します。
    /// </summary>
    [Fact]
    public async Task ProcessAsync_WhenRuntimeFails_ReturnsFailureResult()
    {
        // Arrange
        var completedAt =
            DateTimeOffset.Now;

        var runtimeLauncher =
            new FakeCaptureRuntimeLauncher(
                new CaptureRuntimeLaunchResult(
                    Succeeded: false,
                    ExitCode: 1,
                    StartedAt:
                        completedAt.AddSeconds(-1),
                    CompletedAt: completedAt,
                    ErrorCode: "CAP-E001",
                    ErrorMessage:
                        "撮影Runtimeの起動に失敗しました。"));

        var processor =
            new CaptureRequestProcessor(
                runtimeLauncher);

        var request =
            new CaptureRequest(
                RequestId: 202,
                RequestedAt:
                    DateTimeOffset.Now);

        // Act
        var result =
            await processor.ProcessAsync(
                request);

        // Assert
        Assert.Equal(
            1,
            runtimeLauncher.LaunchCount);

        Assert.Equal(
            202,
            result.RequestId);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            completedAt,
            result.CompletedAt);

        Assert.Equal(
            "CAP-E001",
            result.ErrorCode);

        Assert.Equal(
            "撮影Runtimeの起動に失敗しました。",
            result.ErrorMessage);
    }

    /// <summary>
    /// ProcessAsyncへ渡したCancellationTokenが、
    /// Runtime起動処理へそのまま引き渡されることを確認します。
    /// </summary>
    [Fact]
    public async Task ProcessAsync_PassesCancellationToken()
    {
        // Arrange
        var runtimeLauncher =
            new FakeCaptureRuntimeLauncher(
                CreateSuccessfulLaunchResult());

        var processor =
            new CaptureRequestProcessor(
                runtimeLauncher);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var request =
            new CaptureRequest(
                RequestId: 101,
                RequestedAt:
                    DateTimeOffset.Now);

        // Act
        await processor.ProcessAsync(
            request,
            cancellationTokenSource.Token);

        // Assert
        Assert.Equal(
            cancellationTokenSource.Token,
            runtimeLauncher.LastCancellationToken);
    }

    /// <summary>
    /// CaptureRequestがnullの場合、
    /// ArgumentNullExceptionがスローされることを確認します。
    /// </summary>
    [Fact]
    public async Task ProcessAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var runtimeLauncher =
            new FakeCaptureRuntimeLauncher(
                CreateSuccessfulLaunchResult());

        var processor =
            new CaptureRequestProcessor(
                runtimeLauncher);

        // Act
        var exception =
            await Assert.ThrowsAsync<
                ArgumentNullException>(
                () => processor.ProcessAsync(
                    null!));

        // Assert
        Assert.Equal(
            "request",
            exception.ParamName);
    }

    /// <summary>
    /// ICaptureRuntimeLauncherがnullの場合、
    /// ArgumentNullExceptionがスローされることを確認します。
    /// </summary>
    [Fact]
    public void Constructor_WhenRuntimeLauncherIsNull_ThrowsArgumentNullException()
    {
        // Act
        var exception =
            Assert.Throws<
                ArgumentNullException>(
                () =>
                    new CaptureRequestProcessor(
                        null!));

        // Assert
        Assert.Equal(
            "runtimeLauncher",
            exception.ParamName);
    }

    /// <summary>
    /// 成功したRuntime起動結果を生成します。
    /// </summary>
    private static CaptureRuntimeLaunchResult
        CreateSuccessfulLaunchResult()
    {
        var completedAt =
            DateTimeOffset.Now;

        return new CaptureRuntimeLaunchResult(
            Succeeded: true,
            ExitCode: 0,
            StartedAt:
                completedAt.AddMilliseconds(-100),
            CompletedAt: completedAt,
            ErrorCode: null,
            ErrorMessage: null);
    }

    /// <summary>
    /// テスト用のCapture Runtime起動サービスです。
    /// </summary>
    private sealed class FakeCaptureRuntimeLauncher
        : ICaptureRuntimeLauncher
    {
        private readonly CaptureRuntimeLaunchResult
            _result;

        public FakeCaptureRuntimeLauncher(
            CaptureRuntimeLaunchResult result)
        {
            _result = result;
        }

        /// <summary>
        /// Runtime起動処理が呼び出された回数です。
        /// </summary>
        public int LaunchCount { get; private set; }

        /// <summary>
        /// 最後にLaunchAsyncへ渡されたCancellationTokenです。
        /// </summary>
        public CancellationToken
            LastCancellationToken
        { get; private set; }

        /// <inheritdoc />
        public Task<CaptureRuntimeLaunchResult>
            LaunchAsync(
                CancellationToken cancellationToken =
                    default)
        {
            LaunchCount++;

            LastCancellationToken =
                cancellationToken;

            return Task.FromResult(
                _result);
        }
    }
}