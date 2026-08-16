using LinuxEdgeInspection.Plugin.CameraTest.Models;

namespace LinuxEdgeInspection.Plugin.CameraTest.Services;

/// <summary>
/// InspectionWorker接続前に画面動作を確認するためのダミー実装です。
/// </summary>
public sealed class DummyCameraTestService : ICameraTestService
{
    public Task<CameraTestResult> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new CameraTestResult(
            Capture: "Success",
            Preprocess: "Success",
            Analysis: "Success",
            Judgement: "Ok",
            Label: "DUMMY_OK",
            Error: null));
    }
}
