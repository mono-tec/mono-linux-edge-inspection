using LinuxEdgeInspection.Plugin.LogViewer.Models;
using LinuxEdgeInspection.Plugin.LogViewer.Services;

namespace LinuxEdgeInspection.Management.Tests;

public sealed class JournalApplicationCatalogTests
{
    [Theory]
    [InlineData(LogApplication.Management,
        "linux-edge-inspection-management.service")]
    [InlineData(LogApplication.ManagementApi,
        "linux-edge-inspection-management-api.service")]
    [InlineData(LogApplication.InspectionWorker,
        "linux-edge-inspection-inspection-worker.service")]
    [InlineData(LogApplication.CaptureRequestListener,
        "linux-edge-inspection-capture-request-listener.service")]
    [InlineData(LogApplication.Runtime,
        "linux-edge-inspection-runtime.service")]
    public void Get_ReturnsFixedSystemdUnit(
        LogApplication application,
        string expectedUnit)
    {
        Assert.Equal(
            expectedUnit,
            JournalApplicationCatalog.Get(application).SystemdUnit);
    }

    [Fact]
    public void Get_RejectsUndefinedApplication()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            JournalApplicationCatalog.Get((LogApplication)999));
    }
}
