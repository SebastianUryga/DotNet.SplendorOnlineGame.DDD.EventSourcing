using System.Diagnostics.Metrics;
using FluentAssertions;
using MediatR;
using Splendor.Application.Behaviors;
using Xunit;

namespace Splendor.UnitTests;

public class CommandMetricsBehaviorTests
{
    [Fact]
    public async Task Records_successful_command()
    {
        var measurement = new MetricsMeasurement();
        using var listener = measurement.Start();
        var behavior = new CommandMetricsBehavior<TestCommand, Unit>();

        await behavior.Handle(new TestCommand(), () => Task.FromResult(Unit.Value), CancellationToken.None);

        measurement.Executed.Should().Be(1);
        measurement.Active.Should().Be(0);
        measurement.Command.Should().Be(nameof(TestCommand));
        measurement.Outcome.Should().Be("success");
        measurement.Duration.Should().BeGreaterThanOrEqualTo(0);
    }

    private sealed record TestCommand : IRequest<Unit>;

    private sealed class MetricsMeasurement
    {
        public long Executed { get; private set; }
        public long Active { get; private set; }
        public double Duration { get; private set; }
        public string? Command { get; private set; }
        public string? Outcome { get; private set; }

        public MeterListener Start()
        {
            var listener = new MeterListener
            {
                InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == "Splendor.Application")
                    {
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                }
            };

            listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            {
                if (instrument.Name != "splendor.command.executed")
                {
                    if (instrument.Name != "splendor.command.active") return;
                    Active += value;
                    return;
                }

                Executed += value;
                ReadTags(tags);
            });
            listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            {
                if (instrument.Name != "splendor.command.duration")
                {
                    return;
                }

                Duration = value;
                ReadTags(tags);
            });
            listener.Start();
            return listener;
        }

        private void ReadTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "command") Command = tag.Value?.ToString();
                if (tag.Key == "outcome") Outcome = tag.Value?.ToString();
            }
        }
    }
}
