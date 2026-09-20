using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Marten;
using Npgsql;

namespace Splendor.Infrastructure.Observability;

public class MartenMetricsLogger : IMartenLogger, IMartenSessionLogger
{
    private static readonly Meter Meter = new("Splendor.Infrastructure", "1.0.0");
    private static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        "splendor.marten.command.duration",
        "ms",
        "Marten PostgreSQL command duration.");
    private static readonly Counter<long> CommandsExecuted = Meter.CreateCounter<long>(
        "splendor.marten.command.executed",
        "{command}",
        "Number of Marten PostgreSQL commands.");

    private readonly ConcurrentDictionary<object, long> _startedCommands = new();

    public IMartenSessionLogger StartSession(IQuerySession session) => this;

    public void SchemaChange(string sql)
    {
    }

    public void OnBeforeExecute(NpgsqlCommand command) => Start(command);

    public void OnBeforeExecute(NpgsqlBatch batch) => Start(batch);

    public void LogSuccess(NpgsqlCommand command) => Complete(command, "success");

    public void LogFailure(NpgsqlCommand command, Exception ex) => Complete(command, "error");

    public void LogSuccess(NpgsqlBatch batch) => Complete(batch, "success");

    public void LogFailure(NpgsqlBatch batch, Exception ex) => Complete(batch, "error");

    public void LogFailure(Exception ex, string message)
    {
    }

    public void RecordSavedChanges(IDocumentSession session, Marten.Services.IChangeSet commit)
    {
    }

    private void Start(object command)
    {
        _startedCommands[command] = Stopwatch.GetTimestamp();
    }

    private void Complete(object command, string outcome)
    {
        if (!_startedCommands.TryRemove(command, out var started))
        {
            return;
        }

        var tags = new TagList
        {
            { "operation", GetOperation(command) },
            { "outcome", outcome }
        };

        CommandDuration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, tags);
        CommandsExecuted.Add(1, tags);
    }

    private static string GetOperation(object command)
    {
        var text = command switch
        {
            NpgsqlCommand sqlCommand => sqlCommand.CommandText,
            NpgsqlBatch batch => GetBatchText(batch),
            _ => string.Empty
        };

        if (text.Contains("mt_events", StringComparison.OrdinalIgnoreCase))
        {
            if (text.Contains("insert", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("delete", StringComparison.OrdinalIgnoreCase))
            {
                return "event-write";
            }

            return "event-read";
        }

        if (text.Contains("select", StringComparison.OrdinalIgnoreCase))
        {
            return "document-read";
        }

        return "other";
    }

    private static string GetBatchText(NpgsqlBatch batch)
    {
        var commands = new List<string>();
        foreach (var command in batch.BatchCommands)
        {
            commands.Add(command.CommandText);
        }

        return string.Join('\n', commands);
    }
}
