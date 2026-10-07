using Marten;
using MediatR;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record ExpireTurnCommand(Guid GameId, Guid TurnId, string PlayerId) : IRequest;

public class ExpireTurnCommandHandler : IRequestHandler<ExpireTurnCommand>
{
    private readonly IDocumentSession _session;
    private readonly TimeProvider _timeProvider;

    public ExpireTurnCommandHandler(IDocumentSession session, TimeProvider timeProvider)
    {
        _session = session;
        _timeProvider = timeProvider;
    }

    public async Task Handle(ExpireTurnCommand command, CancellationToken cancellationToken)
    {
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        // Late timer messages (deleted game, clock not started) are no-ops, not errors.
        var state = stream.Aggregate;
        if (state is null) return;
        var turnClock = await _session.Events.FetchForWritingByTags<TurnClockState>(
            TurnClockState.Query(command.GameId), cancellationToken);
        var clock = turnClock.Aggregate;
        if (clock is null) return;

        var events = Decide(command, state, clock, _timeProvider.GetUtcNow());
        if (events.Count == 0) return;

        stream.AppendMany(events.Select(_session.TagEvent));
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<IDomainEvent> Decide(ExpireTurnCommand command, SplendorGameState state, TurnClockState turnClock, DateTimeOffset now)
    {
        if (state.Status != GameStatus.Started ||
            state.CurrentPlayerId != command.PlayerId ||
            !turnClock.IsWaitingForAction ||
            turnClock.TurnId != command.TurnId ||
            turnClock.PlayerId != command.PlayerId ||
            now < turnClock.ExpiresAt)
        {
            return [];
        }

        var events = new List<IDomainEvent>
        {
            new TurnExpired(command.GameId, command.TurnId, command.PlayerId, now)
        };
        events.AddRange(TurnCompletion.DecideAfterExpiration(command.GameId, command.PlayerId, state, now));
        return events;
    }
}
