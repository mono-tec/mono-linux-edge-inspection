using System.Globalization;
using System.Text.Json;
using LinuxEdgeInspection.Plugin.LogViewer.Models;

namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public sealed class JournaldLogViewerService : ILogViewerService
{
    private readonly JournalctlArgumentsBuilder _argumentsBuilder;
    private readonly IJournalctlProcessRunner _processRunner;

    public JournaldLogViewerService(
        JournalctlArgumentsBuilder argumentsBuilder,
        IJournalctlProcessRunner processRunner)
    {
        _argumentsBuilder = argumentsBuilder
            ?? throw new ArgumentNullException(nameof(argumentsBuilder));
        _processRunner = processRunner
            ?? throw new ArgumentNullException(nameof(processRunner));
    }

    public async Task<LogPage> GetLogsAsync(
        LogQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var application = JournalApplicationCatalog.Get(query.Application);
        var command = _argumentsBuilder.Build(query);
        var processResult = await _processRunner.RunAsync(
            command,
            cancellationToken);

        if (processResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"journalctl failed with exit code {processResult.ExitCode}: " +
                processResult.StandardError.Trim());
        }

        var rawEntries = ParseEntries(
            processResult.StandardOutput,
            application.Component);

        if (query.Direction == LogPageDirection.Older &&
            query.Cursor is not null)
        {
            rawEntries.RemoveAll(entry => entry.Cursor == query.Cursor);
        }

        var hasAdditionalEntries =
            rawEntries.Count > JournalctlArgumentsBuilder.PageSize;
        var rawPage = rawEntries
            .Take(JournalctlArgumentsBuilder.PageSize)
            .OrderByDescending(entry => entry.Timestamp)
            .ToArray();
        var entries = rawPage
            .Where(entry => MatchesLevel(entry.Level, query.Level))
            .OrderByDescending(entry => entry.Timestamp)
            .ToArray();

        return new LogPage(
            entries,
            rawPage.LastOrDefault()?.Cursor,
            rawPage.FirstOrDefault()?.Cursor,
            CanLoadOlder: query.Direction == LogPageDirection.Newer ||
                          hasAdditionalEntries,
            CanLoadNewer: query.Direction == LogPageDirection.Older ||
                          (query.Direction == LogPageDirection.Newer &&
                           hasAdditionalEntries));
    }

    private static List<LogEntry> ParseEntries(
        string output,
        string component)
    {
        var entries = new List<LogEntry>();
        using var reader = new StringReader(output);

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!TryGetString(root, "__CURSOR", out var cursor) ||
                !TryGetString(root, "__REALTIME_TIMESTAMP", out var timestamp) ||
                !long.TryParse(timestamp, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var microseconds))
            {
                continue;
            }

            TryGetString(root, "MESSAGE", out var message);
            TryGetString(root, "PRIORITY", out var priority);
            entries.Add(new LogEntry(
                DateTimeOffset.FromUnixTimeMilliseconds(microseconds / 1000)
                    .ToLocalTime(),
                ClassifyLevel(message, priority),
                component,
                message ?? string.Empty,
                cursor!));
        }

        return entries;
    }

    internal static string ClassifyLevel(string? message, string? priority)
    {
        var text = message?.TrimStart() ?? string.Empty;
        if (text.StartsWith("warn:", StringComparison.OrdinalIgnoreCase))
        {
            return "Warning";
        }

        if (text.StartsWith("fail:", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("crit:", StringComparison.OrdinalIgnoreCase))
        {
            return "Error";
        }

        if (text.StartsWith("info:", StringComparison.OrdinalIgnoreCase))
        {
            return "Information";
        }

        return int.TryParse(priority, out var value) ? value switch
        {
            <= 3 => "Error",
            4 => "Warning",
            _ => "Information"
        } : "Information";
    }

    private static bool MatchesLevel(string level, LogLevelFilter filter) =>
        filter == LogLevelFilter.All ||
        string.Equals(level, filter.ToString(), StringComparison.Ordinal);

    private static bool TryGetString(
        JsonElement element,
        string propertyName,
        out string? value)
    {
        value = null;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        value = property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
        return value is not null;
    }
}
