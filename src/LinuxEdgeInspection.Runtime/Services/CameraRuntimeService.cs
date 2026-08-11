using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.Abstractions.Services;

namespace LinuxEdgeInspection.Runtime.Services;

/// <summary>
/// カメラの環境確認、開始、撮影、停止をまとめて実行します。
/// </summary>
public sealed class CameraRuntimeService
    : ICameraRuntimeService
{
    private readonly ICameraService _cameraService;
    private readonly CameraOptions _cameraOptions;

    /// <summary>
    /// <see cref="CameraRuntimeService"/>を初期化します。
    /// </summary>
    /// <param name="cameraService">
    /// カメラ操作サービスです。
    /// </param>
    /// <param name="cameraOptions">
    /// カメラ設定です。
    /// </param>
    public CameraRuntimeService(
        ICameraService cameraService,
        CameraOptions cameraOptions)
    {
        _cameraService = cameraService
            ?? throw new ArgumentNullException(
                nameof(cameraService));

        _cameraOptions = cameraOptions
            ?? throw new ArgumentNullException(
                nameof(cameraOptions));
    }

    /// <inheritdoc />
    public async Task RunAsync(
    CancellationToken cancellationToken = default)
    {
        var cameraStarted = false;

        try
        {
            Console.WriteLine(
                "Linux Edge Inspection Runtimeを起動しました。");

            Console.WriteLine(
                $"Camera Device : {_cameraOptions.DevicePath}");

            Console.WriteLine(
                $"Output        : {_cameraOptions.OutputDirectory}");

            Console.WriteLine();

            Console.WriteLine(
                "カメラ環境を確認しています。");

            var status =
                await _cameraService.GetStatusAsync(
                    cancellationToken);

            WriteStatus(status);

            if (!status.IsDeviceConnected ||
                !status.IsCommandAvailable)
            {
                Console.WriteLine();

                Console.WriteLine(
                    "カメラを利用できる環境ではないため、撮影処理を終了します。");

                return;
            }

            Console.WriteLine();

            Console.WriteLine(
                "カメラ機能を開始します。");

            await _cameraService.StartAsync(
                cancellationToken);

            cameraStarted = true;

            Console.WriteLine(
                "画像を撮影します。");

            var captureResult =
                await _cameraService.CaptureAsync(
                    new CameraCaptureRequest(),
                    cancellationToken);

            WriteCaptureResult(captureResult);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine(
                "カメラ処理がキャンセルされました。");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                "カメラ処理中にエラーが発生しました。");

            Console.Error.WriteLine(
                exception.Message);

            Environment.ExitCode = 1;
        }
        finally
        {
            if (cameraStarted)
            {
                await StopCameraAsync();
            }
        }
    }

    private static void WriteStatus(
        CameraStatus status)
    {
        Console.WriteLine(
            $"Device Connected : {status.IsDeviceConnected}");

        Console.WriteLine(
            $"Command Available: {status.IsCommandAvailable}");

        Console.WriteLine(
            $"Ready            : {status.IsReady}");

        Console.WriteLine(
            $"Capturing        : {status.IsCapturing}");

        Console.WriteLine(
            $"Device Path      : {status.DevicePath}");

        Console.WriteLine(
            $"Message          : {status.Message}");
    }

    private static void WriteCaptureResult(
        CameraCaptureResult captureResult)
    {
        Console.WriteLine();

        if (captureResult.Succeeded)
        {
            Console.WriteLine(
                "撮影に成功しました。");

            Console.WriteLine(
                $"File Path : {captureResult.FilePath}");

            Console.WriteLine(
                $"File Size : {captureResult.FileSize}");

            Console.WriteLine(
                $"Duration  : {captureResult.Duration}");

            return;
        }

        Console.Error.WriteLine(
            "撮影に失敗しました。");

        Console.Error.WriteLine(
            $"Error Code   : {captureResult.ErrorCode}");

        Console.Error.WriteLine(
            $"Error Message: {captureResult.ErrorMessage}");

        Environment.ExitCode = 1;
    }

    private async Task StopCameraAsync()
    {
        try
        {
            await _cameraService.StopAsync();

            Console.WriteLine();

            Console.WriteLine(
                "カメラ機能を停止しました。");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                "カメラ機能の停止中にエラーが発生しました。");

            Console.Error.WriteLine(
                exception.Message);

            Environment.ExitCode = 1;
        }
    }
}