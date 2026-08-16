using LinuxEdgeInspection.Plugin.LogViewer.Models;
using LinuxEdgeInspection.Plugin.LogViewer.Options;
using LinuxEdgeInspection.Plugin.LogViewer.Services;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.Management.Tests;

public sealed class JournalctlArgumentsBuilderTests
{
    [Fact]
    public void Build_UsesUnitDateJsonAndBoundedLineCount()
    {
        var builder = CreateBuilder();

        var command = builder.Build(new LogQuery(
            LogApplication.Runtime,
            new DateOnly(2026, 8, 16),
            LogLevelFilter.All));

        Assert.Equal("/usr/bin/journalctl", command.ExecutablePath);
        AssertOption(command.Arguments, "--unit",
            "linux-edge-inspection-runtime.service");
        AssertOption(command.Arguments, "--since", "2026-08-16 00:00:00");
        AssertOption(command.Arguments, "--until", "2026-08-17 00:00:00");
        AssertOption(command.Arguments, "--output", "json");
        AssertOption(command.Arguments, "--lines", "101");
        Assert.Contains("--reverse", command.Arguments);
    }

    [Fact]
    public void Build_OlderQuery_UsesOpaqueCursorAsSeparateArgument()
    {
        var builder = CreateBuilder();
        const string cursor = "s=abc;i=123;b=456";

        var command = builder.Build(new LogQuery(
            LogApplication.Management,
            new DateOnly(2026, 8, 16),
            LogLevelFilter.Warning,
            cursor,
            LogPageDirection.Older));

        AssertOption(command.Arguments, "--cursor", cursor);
        AssertOption(command.Arguments, "--lines", "102");
        Assert.Contains("--reverse", command.Arguments);
    }

    [Fact]
    public void Build_NewerQuery_UsesAfterCursorWithoutReverse()
    {
        var builder = CreateBuilder();
        const string cursor = "s=abc;i=123";

        var command = builder.Build(new LogQuery(
            LogApplication.Management,
            new DateOnly(2026, 8, 16),
            LogLevelFilter.Error,
            cursor,
            LogPageDirection.Newer));

        AssertOption(command.Arguments, "--after-cursor", cursor);
        Assert.DoesNotContain("--reverse", command.Arguments);
    }

    [Fact]
    public void Build_RejectsCursorContainingControlCharacters()
    {
        var builder = CreateBuilder();

        Assert.Throws<ArgumentException>(() => builder.Build(new LogQuery(
            LogApplication.Management,
            new DateOnly(2026, 8, 16),
            LogLevelFilter.All,
            "cursor\n--unit=ssh.service",
            LogPageDirection.Older)));
    }

    private static JournalctlArgumentsBuilder CreateBuilder() =>
        new(Options.Create(new JournalctlOptions
        {
            ExecutablePath = "/usr/bin/journalctl",
            TimeoutSeconds = 10
        }));

    private static void AssertOption(
        IReadOnlyList<string> arguments,
        string option,
        string value)
    {
        var index = arguments.IndexOf(option);
        Assert.True(index >= 0, $"Missing option {option}.");
        Assert.Equal(value, arguments[index + 1]);
    }
}

internal static class ReadOnlyListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> values, T value)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(values[index], value))
            {
                return index;
            }
        }

        return -1;
    }
}
