using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.Abstractions.Services;
using LinuxEdgeInspection.Camera.V4L2.Models;
using System.Diagnostics;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// v4l2-ctlを利用してUSBカメラを操作します。
/// </summary>
public sealed class V4L2CameraService : ICameraService, IDisposable
{
    private const string CameraBusyErrorCode = "CAM-E005";
    private const string TimeoutErrorCode = "CAM-E006";
    private const string ProcessErrorCode = "CAM-E007";
    private const string FileErrorCode = "CAM-E008";
    private const string InvalidCaptureFileErrorCode = "CAM-E009";
    private const string CancelledErrorCode = "CAM-E010";

    private readonly CameraOptions _options;
    private readonly ICameraEnvironmentService _environmentService;
    private readonly IV4L2CommandBuilder _commandBuilder;
    private readonly ICameraFileService _fileService;
    private readonly ICameraProcessRunner _processRunner;
    private readonly ICameraStateManager _stateManager;

    private readonly SemaphoreSlim _captureLock = new(1, 1);
    private readonly object _cancellationSyncRoot = new();

    private CancellationTokenSource? _activeCaptureCancellationTokenSource;
    private bool _disposed;

    /// <summary>
    /// <see cref="V4L2CameraService"/>を初期化します。
    /// </summary>
    /// <param name="options">
    /// カメラの撮影設定です。
    /// </param>
    /// <param name="environmentService">
    /// カメラデバイスとv4l2-ctlの利用環境を確認する機能です。
    /// </param>
    /// <param name="commandBuilder">
    /// v4l2-ctlへ渡す引数を生成する機能です。
    /// </param>
    /// <param name="fileService">
    /// 撮影画像の保存とファイル検証を行う機能です。
    /// </param>
    /// <param name="processRunner">
    /// v4l2-ctlを外部プロセスとして実行する機能です。
    /// </param>
    /// <param name="stateManager">
    /// カメラ機能の内部状態を管理する機能です。
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// いずれかの引数が<c>null</c>の場合にスローされます。
    /// </exception>
    public V4L2CameraService(
        CameraOptions options,
        ICameraEnvironmentService environmentService,
        IV4L2CommandBuilder commandBuilder,
        ICameraFileService fileService,
        ICameraProcessRunner processRunner,
        ICameraStateManager stateManager)
    {
        _options = options
            ?? throw new ArgumentNullException(nameof(options));

        _environmentService = environmentService
            ?? throw new ArgumentNullException(nameof(environmentService));

        _commandBuilder = commandBuilder
            ?? throw new ArgumentNullException(nameof(commandBuilder));

        _fileService = fileService
            ?? throw new ArgumentNullException(nameof(fileService));

        _processRunner = processRunner
            ?? throw new ArgumentNullException(nameof(processRunner));

        _stateManager = stateManager
            ?? throw new ArgumentNullException(nameof(stateManager));
    }

    /// <inheritdoc />
    public async Task<CameraStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var environmentStatus =
            await _environmentService.CheckAsync(
                _options,
                cancellationToken);

        var currentState = _stateManager.CurrentState;

        return new CameraStatus(
            IsDeviceConnected: environmentStatus.DeviceExists,
            IsCommandAvailable: environmentStatus.CommandAvailable,
            IsReady:
                environmentStatus.IsReady &&
                currentState == CameraState.Ready,
            IsCapturing:
                currentState == CameraState.Capturing,
            DevicePath: environmentStatus.DevicePath,
            Message: environmentStatus.Message);
    }

    /// <inheritdoc />
    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_stateManager.CurrentState == CameraState.Capturing)
        {
            throw new InvalidOperationException(
                "撮影中のため、カメラ機能を開始できません。");
        }

        var environmentStatus =
            await _environmentService.CheckAsync(
                _options,
                cancellationToken);

        if (!environmentStatus.IsReady)
        {
            TransitionToFaulted();

            throw new InvalidOperationException(
                environmentStatus.Message
                ?? "カメラを利用できる環境ではありません。");
        }

        _fileService.EnsureOutputDirectory(
            _options.OutputDirectory);

        _stateManager.TransitionTo(CameraState.Ready);
    }

    /// <inheritdoc />
    public async Task<CameraCaptureResult> CaptureAsync(
        CameraCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);

        if (_stateManager.CurrentState == CameraState.Capturing)
        {
            return CreateFailureResult(
                CameraBusyErrorCode,
                "カメラは撮影処理中です。",
                TimeSpan.Zero);
        }


        if (_stateManager.CurrentState != CameraState.Ready)
        {
            throw new InvalidOperationException(
                "カメラが撮影可能な状態ではありません。先にStartAsyncを実行してください。");
        }

        var lockAcquired = await _captureLock.WaitAsync(
            millisecondsTimeout: 0,
            cancellationToken);

        if (!lockAcquired)
        {
            return CreateFailureResult(
                CameraBusyErrorCode,
                "カメラは撮影処理中です。",
                TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        string? temporaryPath = null;

        try
        {
            _stateManager.TransitionTo(
                CameraState.Capturing);

            var effectiveOptions =
                CreateEffectiveOptions(request);

            var outputPath =
                CreateOutputPath(request);

            var outputDirectory =
                Path.GetDirectoryName(outputPath);

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new IOException(
                    "画像の保存先ディレクトリを取得できません。");
            }

            _fileService.EnsureOutputDirectory(
                outputDirectory);

            temporaryPath =
                _fileService.CreateTemporaryPath(
                    outputPath);

            _fileService.DeleteTemporaryFile(
                temporaryPath);

            var arguments =
                _commandBuilder.BuildCaptureArguments(
                    effectiveOptions,
                    temporaryPath);

            var processRequest =
                new ProcessExecutionRequest(
                    FileName:
                        effectiveOptions.V4L2CommandPath,
                    Arguments:
                        arguments,
                    Timeout:
                        TimeSpan.FromSeconds(
                            effectiveOptions.CaptureTimeoutSeconds));

            using var captureCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            SetActiveCancellationTokenSource(
                captureCancellationTokenSource);

            var processResult =
                await _processRunner.ExecuteAsync(
                    processRequest,
                    captureCancellationTokenSource.Token);

            if (processResult.TimedOut)
            {
                return CreateFailureResult(
                    TimeoutErrorCode,
                    "カメラの撮影処理がタイムアウトしました。",
                    processResult.Duration);
            }

            if (processResult.Cancelled)
            {
                return CreateFailureResult(
                    CancelledErrorCode,
                    "カメラの撮影処理がキャンセルされました。",
                    processResult.Duration);
            }

            if (!processResult.Succeeded)
            {
                var message =
                    string.IsNullOrWhiteSpace(
                        processResult.StandardError)
                        ? "v4l2-ctlが異常終了しました。"
                        : processResult.StandardError.Trim();

                return CreateFailureResult(
                    ProcessErrorCode,
                    message,
                    processResult.Duration);
            }

            if (!_fileService.IsValidCaptureFile(
                    temporaryPath))
            {
                return CreateFailureResult(
                    InvalidCaptureFileErrorCode,
                    "撮影された画像ファイルが存在しないか、ファイルサイズが0です。",
                    processResult.Duration);
            }

            _fileService.MoveToOutput(
                temporaryPath,
                outputPath);

            temporaryPath = null;

            var fileSize =
                _fileService.GetFileSize(
                    outputPath);

            stopwatch.Stop();

            return new CameraCaptureResult(
                Succeeded: true,
                FilePath: outputPath,
                FileSize: fileSize,
                CapturedAt: DateTimeOffset.Now,
                Duration: stopwatch.Elapsed,
                DevicePath: effectiveOptions.DevicePath,
                ErrorCode: null,
                ErrorMessage: null);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (IOException exception)
        {
            stopwatch.Stop();

            return CreateFailureResult(
                FileErrorCode,
                exception.Message,
                stopwatch.Elapsed);
        }
        catch (UnauthorizedAccessException exception)
        {
            stopwatch.Stop();

            return CreateFailureResult(
                FileErrorCode,
                exception.Message,
                stopwatch.Elapsed);
        }
        finally
        {
            ClearActiveCancellationTokenSource();

            if (!string.IsNullOrWhiteSpace(
                    temporaryPath))
            {
                _fileService.DeleteTemporaryFile(
                    temporaryPath);
            }

            if (_stateManager.CurrentState ==
                CameraState.Capturing)
            {
                _stateManager.TransitionTo(
                    CameraState.Ready);
            }

            _captureLock.Release();
        }
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        CancelActiveCapture();

        var currentState =
            _stateManager.CurrentState;

        if (currentState != CameraState.Stopped)
        {
            _stateManager.TransitionTo(
                CameraState.Stopped);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CancelActiveCapture();

        lock (_cancellationSyncRoot)
        {
            _activeCaptureCancellationTokenSource?.Dispose();
            _activeCaptureCancellationTokenSource = null;
        }

        _captureLock.Dispose();
        _disposed = true;
    }

    private CameraOptions CreateEffectiveOptions(
        CameraCaptureRequest request)
    {
        return new CameraOptions
        {
            DevicePath = _options.DevicePath,
            Width = request.Width
                ?? _options.Width,
            Height = request.Height
                ?? _options.Height,
            PixelFormat = request.PixelFormat
                ?? _options.PixelFormat,
            FramesPerSecond = request.FramesPerSecond
                ?? _options.FramesPerSecond,
            SkipFrames = request.SkipFrames
                ?? _options.SkipFrames,
            OutputDirectory =
                _options.OutputDirectory,
            CaptureTimeoutSeconds =
                _options.CaptureTimeoutSeconds,
            V4L2CommandPath =
                _options.V4L2CommandPath
        };
    }

    private string CreateOutputPath(
        CameraCaptureRequest request)
    {
        if (!string.IsNullOrWhiteSpace(
                request.OutputPath))
        {
            return request.OutputPath;
        }

        return _fileService.CreateOutputPath(
            _options.OutputDirectory,
            DateTimeOffset.Now);
    }

    private CameraCaptureResult CreateFailureResult(
        string errorCode,
        string errorMessage,
        TimeSpan duration)
    {
        return new CameraCaptureResult(
            Succeeded: false,
            FilePath: null,
            FileSize: 0,
            CapturedAt: DateTimeOffset.Now,
            Duration: duration,
            DevicePath: _options.DevicePath,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
    }

    private void TransitionToFaulted()
    {
        if (_stateManager.CurrentState !=
            CameraState.Faulted)
        {
            _stateManager.TransitionTo(
                CameraState.Faulted);
        }
    }

    private void SetActiveCancellationTokenSource(
        CancellationTokenSource cancellationTokenSource)
    {
        lock (_cancellationSyncRoot)
        {
            _activeCaptureCancellationTokenSource =
                cancellationTokenSource;
        }
    }

    private void ClearActiveCancellationTokenSource()
    {
        lock (_cancellationSyncRoot)
        {
            _activeCaptureCancellationTokenSource =
                null;
        }
    }

    private void CancelActiveCapture()
    {
        lock (_cancellationSyncRoot)
        {
            if (_activeCaptureCancellationTokenSource
                is { IsCancellationRequested: false })
            {
                _activeCaptureCancellationTokenSource.Cancel();
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}