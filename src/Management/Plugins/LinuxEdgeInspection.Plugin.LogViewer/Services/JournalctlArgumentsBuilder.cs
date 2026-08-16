using System.Globalization;
using LinuxEdgeInspection.Plugin.LogViewer.Models;
using LinuxEdgeInspection.Plugin.LogViewer.Options;
using Microsoft.Extensions.Options;

namespace LinuxEdgeInspection.Plugin.LogViewer.Services;

public sealed class JournalctlArgumentsBuilder
{
    public const int PageSize = 100;

    private readonly JournalctlOptions _options;

    public JournalctlArgumentsBuilder(IOptions<JournalctlOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value
            ?? throw new ArgumentNullException(nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.ExecutablePath);
    }

    public JournalctlCommand Build(LogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateCursor(query);

        var application = JournalApplicationCatalog.Get(query.Application);
        var since = query.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            + " 00:00:00";
        var until = query.Date.AddDays(1)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            + " 00:00:00";

        var arguments = new List<string>
        {
            "--unit", application.SystemdUnit,
            "--since", since,
            "--until", until,
            "--output", "json",
            "--no-pager",
            "--lines", (PageSize +
                (query.Direction == LogPageDirection.Older ? 2 : 1))
                .ToString(CultureInfo.InvariantCulture)
        };

        if (query.Direction is LogPageDirection.Initial or LogPageDirection.Older)
        {
            arguments.Add("--reverse");
        }

        if (query.Direction == LogPageDirection.Older)
        {
            arguments.Add("--cursor");
            arguments.Add(query.Cursor!);
        }
        else if (query.Direction == LogPageDirection.Newer)
        {
            arguments.Add("--after-cursor");
            arguments.Add(query.Cursor!);
        }

        return new JournalctlCommand(_options.ExecutablePath, arguments);
    }

    private static void ValidateCursor(LogQuery query)
    {
        if (query.Direction == LogPageDirection.Initial)
        {
            if (query.Cursor is not null)
            {
                throw new ArgumentException(
                    "Initial queries must not specify a cursor.",
                    nameof(query));
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(query.Cursor) ||
            query.Cursor.Length > 4096 ||
            query.Cursor.Contains('\0') ||
            query.Cursor.Contains('\r') ||
            query.Cursor.Contains('\n'))
        {
            throw new ArgumentException(
                "A valid journald cursor is required.",
                nameof(query));
        }
    }
}
