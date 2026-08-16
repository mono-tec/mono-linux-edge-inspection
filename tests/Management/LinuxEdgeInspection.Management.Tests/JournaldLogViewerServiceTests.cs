using System.Text.Json;
using LinuxEdgeInspection.Plugin.LogViewer.Models;
using LinuxEdgeInspection.Plugin.LogViewer.Options;
using LinuxEdgeInspection.Plugin.LogViewer.Services;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.Management.Tests;

public sealed class JournaldLogViewerServiceTests
{
    [Fact]
    public async Task GetLogsAsync_UsesPrefixThenPriorityForLevelFilter()
    {
        var output = string.Join(Environment.NewLine,
            CreateJson("c1", 1, "warn: Component[0] warning", "6"),
            CreateJson("c2", 2, "plain error", "3"),
            CreateJson("c3", 3, "info: Component[0] info", "3"));
        var runner = new StubRunner(output);
        var service = CreateService(runner);

        var warningPage = await service.GetLogsAsync(new LogQuery(
            LogApplication.Management,
            new DateOnly(2026, 8, 16),
            LogLevelFilter.Warning));
        var errorPage = await service.GetLogsAsync(new LogQuery(
            LogApplication.Management,
            new DateOnly(2026, 8, 16),
            LogLevelFilter.Error));

        Assert.Equal("c1", Assert.Single(warningPage.Entries).Cursor);
        Assert.Equal("c2", Assert.Single(errorPage.Entries).Cursor);
        Assert.DoesNotContain(errorPage.Entries, entry => entry.Cursor == "c3");
    }

    [Fact]
    public async Task GetLogsAsync_LimitsBrowserResultToOneHundred()
    {
        var lines = Enumerable.Range(1, 101)
            .Select(index => CreateJson(
                $"c{index}",
                102 - index,
                $"info: message {index}",
                "6"));
        var service = CreateService(new StubRunner(
            string.Join(Environment.NewLine, lines)));

        var page = await service.GetLogsAsync(new LogQuery(
            LogApplication.Runtime,
            new DateOnly(2026, 8, 16),
            LogLevelFilter.All));

        Assert.Equal(100, page.Entries.Count);
        Assert.True(page.CanLoadOlder);
        Assert.False(page.CanLoadNewer);
        Assert.Equal("c1", page.NewestCursor);
        Assert.Equal("c100", page.OldestCursor);
    }

    [Fact]
    public async Task GetLogsAsync_OlderPage_RemovesBoundaryAndEnablesNewer()
    {
        var output = string.Join(Environment.NewLine,
            CreateJson("anchor", 3, "info: anchor", "6"),
            CreateJson("older-1", 2, "info: older one", "6"),
            CreateJson("older-2", 1, "info: older two", "6"));
        var service = CreateService(new StubRunner(output));

        var page = await service.GetLogsAsync(new LogQuery(
            LogApplication.Management,
            new DateOnly(2026, 8, 16),
            LogLevelFilter.All,
            "anchor",
            LogPageDirection.Older));

        Assert.Equal(2, page.Entries.Count);
        Assert.DoesNotContain(page.Entries, entry => entry.Cursor == "anchor");
        Assert.True(page.CanLoadNewer);
    }

    private static JournaldLogViewerService CreateService(
        IJournalctlProcessRunner runner)
    {
        var options = Options.Create(new JournalctlOptions
        {
            ExecutablePath = "/usr/bin/journalctl",
            TimeoutSeconds = 10
        });
        return new JournaldLogViewerService(
            new JournalctlArgumentsBuilder(options),
            runner);
    }

    private static string CreateJson(
        string cursor,
        long milliseconds,
        string message,
        string priority) =>
        JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["__CURSOR"] = cursor,
            ["__REALTIME_TIMESTAMP"] = (milliseconds * 1000).ToString(),
            ["MESSAGE"] = message,
            ["PRIORITY"] = priority
        });

    private sealed class StubRunner(string output) : IJournalctlProcessRunner
    {
        public Task<JournalctlProcessResult> RunAsync(
            JournalctlCommand command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new JournalctlProcessResult(0, output, string.Empty));
    }
}
