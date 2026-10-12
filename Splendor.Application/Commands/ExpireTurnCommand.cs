using Marten;
using MediatR;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
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

        var now = _timeProvider.GetUtcNow();
        var events = Decide(command, state, clock, now).ToList();
        if (events.Count == 0) return;

        // Apply decision events to local state
        state.Apply(events);

        // Turn completion may produce additional events; merge them
        events.AddRange(DecideCompletion(events, state, clock, now));

        // Tag and append events to the stream
        stream.AppendMany(events.Select(e => _session.TagEvent(e, state.GameCreatorId)));
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

        // A player who ran out of time while a decision was pending gets it made for them.
        if (state.PendingGemReturnPlayerId == command.PlayerId)
        {
            var returned = SplendorRules.ChooseGemsToReturn(state.Players[command.PlayerId].Gems);
            events.Add(new GemLimitResolved(command.GameId, command.PlayerId, returned, now));
        }
        else if (state.PendingNobleSelectionPlayerId == command.PlayerId)
        {
            var nobleId = state.EligibleNobleIds.Order().First();
            events.Add(new NobleAcquired(command.GameId, command.PlayerId, nobleId, now));
        }

        return events;
    }

    /// <summary>
    /// After an automatic gem return the player may still be owed a noble, so the usual end-of-action checks run.
    /// Otherwise (nothing pending, or a noble was just picked) the turn simply ends.
    /// </summary>
    internal static IReadOnlyList<IDomainEvent> DecideCompletion(
        IReadOnlyList<IDomainEvent> decisionEvents, SplendorGameState state, TurnClockState turnClock, DateTimeOffset now) =>
        decisionEvents.OfType<GemLimitResolved>().Any()
            ? TurnCompletion.DecideAfterAction(state, turnClock, now).ToList()
            : TurnCompletion.DecideAfterExpiration(state, turnClock, now).ToList();
}
