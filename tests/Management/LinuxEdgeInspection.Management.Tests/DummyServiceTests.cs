using LinuxEdgeInspection.Plugin.CameraTest.Services;
using LinuxEdgeInspection.Plugin.LogViewer.Services;
using LinuxEdgeInspection.Plugin.LogViewer.Models;

namespace LinuxEdgeInspection.Management.Tests;

public sealed class DummyServiceTests
{
    [Fact]
    public async Task CameraTestService_ReturnsExpectedDummyResult()
    {
        var service = new DummyCameraTestService();

        var result = await service.RunAsync(CancellationToken.None);

        Assert.Equal("Success", result.Capture);
        Assert.Equal("Success", result.Preprocess);
        Assert.Equal("Success", result.Analysis);
        Assert.Equal("Ok", result.Judgement);
        Assert.Equal("DUMMY_OK", result.Label);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task LogViewerService_ReturnsExpectedDummyLogs()
    {
        var service = new DummyLogViewerService();

        var page = await service.GetLogsAsync(new LogQuery(
            LogApplication.Management,
            DateOnly.FromDateTime(DateTime.Today),
            LogLevelFilter.All));
        var logs = page.Entries;

        Assert.Equal(4, logs.Count);
        Assert.All(logs, entry =>
        {
            Assert.NotEqual(default, entry.Timestamp);
            Assert.False(string.IsNullOrWhiteSpace(entry.Level));
            Assert.False(string.IsNullOrWhiteSpace(entry.Component));
            Assert.False(string.IsNullOrWhiteSpace(entry.Message));
        });
        Assert.Contains(logs, entry => entry.Level == "Warning");
        Assert.Contains(logs, entry => entry.Component == "Inspection");
    }
}
