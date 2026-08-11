using LinuxEdgeInspection.Camera.Abstractions.Models;
using LinuxEdgeInspection.Camera.V4L2.Models;

namespace LinuxEdgeInspection.Camera.V4L2.Services;

/// <summary>
/// カメラデバイスとv4l2-ctlコマンドの利用可否を確認します。
/// </summary>
public sealed class CameraEnvironmentService : ICameraEnvironmentService
{
    private readonly ICameraProcessRunner _processRunner;
    private readonly ICameraDeviceFileSystem _deviceFileSystem;

    /// <summary>
    /// <see cref="CameraEnvironmentService"/>を初期化します。
    /// </summary>
    /// <param name="processRunner">
    /// 外部プロセスを実行する機能です。
    /// </param>
    /// <param name="deviceFileSystem">
    /// カメラデバイスファイルの存在およびアクセス権限を確認する機能です。
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="processRunner"/>または
    /// <paramref name="deviceFileSystem"/>が<c>null</c>の場合にスローされます。
    /// </exception>
    public CameraEnvironmentService(
        ICameraProcessRunner processRunner,
        ICameraDeviceFileSystem deviceFileSystem)
    {
        _processRunner = processRunner
            ?? throw new ArgumentNullException(nameof(processRunner));

        _deviceFileSystem = deviceFileSystem
            ?? throw new ArgumentNullException(nameof(deviceFileSystem));
    }

    /// <inheritdoc />
    public async Task<CameraEnvironmentStatus> CheckAsync(
        CameraOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var accessStatus =
            _deviceFileSystem.CheckAccess(options.DevicePath);

        var commandAvailable = await IsCommandAvailableAsync(
            options.V4L2CommandPath,
            cancellationToken);

        var message = CreateMessage(
            accessStatus.Exists,
            accessStatus.Readable,
            accessStatus.Writable,
            commandAvailable);

        return new CameraEnvironmentStatus(
            DeviceExists: accessStatus.Exists,
            DeviceReadable: accessStatus.Readable,
            DeviceWritable: accessStatus.Writable,
            CommandAvailable: commandAvailable,
            DevicePath: options.DevicePath,
            CommandPath: options.V4L2CommandPath,
            Message: message);
    }

    private async Task<bool> IsCommandAvailableAsync(
        string commandPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(commandPath))
        {
            return false;
        }

        var request = new ProcessExecutionRequest(
            FileName: commandPath,
            Arguments: ["--version"],
            Timeout: TimeSpan.FromSeconds(3));

        var result = await _processRunner.ExecuteAsync(
            request,
            cancellationToken);

        return result.Succeeded;
    }

    private static string CreateMessage(
        bool deviceExists,
        bool deviceReadable,
        bool deviceWritable,
        bool commandAvailable)
    {
        if (!deviceExists)
        {
            return "カメラデバイスが見つかりません。";
        }

        if (!deviceReadable || !deviceWritable)
        {
            return "カメラデバイスへアクセスする権限がありません。";
        }

        if (!commandAvailable)
        {
            return "v4l2-ctlコマンドを利用できません。";
        }

        return "カメラを利用できる環境です。";
    }
}