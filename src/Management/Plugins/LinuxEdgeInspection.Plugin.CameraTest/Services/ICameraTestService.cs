using LinuxEdgeInspection.Plugin.CameraTest.Models;

namespace LinuxEdgeInspection.Plugin.CameraTest.Services;

public interface ICameraTestService
{
    Task<CameraTestResult> RunAsync(CancellationToken cancellationToken = default);
}
