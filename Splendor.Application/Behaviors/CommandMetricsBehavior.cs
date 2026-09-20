using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;

namespace Splendor.Application.Behaviors;

public class CommandMetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Meter Meter = new("Splendor.Application", "1.0.0");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "splendor.command.duration",
        "ms",
        "Command execution duration.");
    private static readonly Counter<long> Executed = Meter.CreateCounter<long>(
        "splendor.command.executed",
        "{command}",
        "Number of executed commands.");
    private static readonly UpDownCounter<long> Active = Meter.CreateUpDownCounter<long>(
        "splendor.command.active",
        "{command}",
        "Number of commands currently executing.");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var command = typeof(TRequest).Name;
        var outcome = "error";
        var stopwatch = Stopwatch.StartNew();
        var activeTags = new TagList { { "command", command } };
        Active.Add(1, activeTags);

        try
        {
            var response = await next();
            outcome = "success";
            return response;
        }
        catch (OperationCanceledException)
        {
            outcome = "canceled";
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var tags = new TagList
            {
                { "command", command },
                { "outcome", outcome }
            };

            Duration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
            Executed.Add(1, tags);
            Active.Add(-1, activeTags);
        }
    }
}
