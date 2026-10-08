using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Application.Snapshots;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.Rules;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record ReserveCardCommand : IAuthorizedCommand, IRequest
{
    public Guid GameId { get; init; }
    public required Caller Caller { get; init; }
    public string PlayerId { get; init; } = string.Empty;
    // Null CardId means a blind deck reservation; Level selects the deck.
    public string? CardId { get; init; }
    public int? Level { get; init; }
}

public class ReserveCardCommandHandler : IRequestHandler<ReserveCardCommand>
{
    private readonly IDocumentSession _session;

    public ReserveCardCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(ReserveCardCommand command, CancellationToken cancellationToken)
    {
        var stream = await _session.Events.FetchForWriting<SplendorGameState>(command.GameId, cancellationToken);
        var state = stream.Aggregate ?? throw new InvalidOperationException("Game not found.");
        var turnClock = await _session.Events.FetchForWritingByTags<TurnClockState>(
            TurnClockState.Query(command.GameId), cancellationToken);
        var clock = turnClock.Aggregate ?? throw new InvalidOperationException("Turn clock not started.");

        var events = Decide(command, state, clock).ToList();

        // Apply decision events to local state
        state.Apply(events);

        // Turn completion may produce additional events; merge them
        var completionEvents = TurnCompletion.DecideAfterAction(command.GameId, command.PlayerId, state, DateTimeOffset.UtcNow);
        events.AddRange(completionEvents);

        // Tag and append events to the stream
        stream.AppendMany(events.Select(e => _session.TagEvent(e, state.CreatorId)));
        await _session.SaveChangesAsync(cancellationToken);
    }



    internal static IReadOnlyList<IDomainEvent> Decide(ReserveCardCommand command, SplendorGameState state, TurnClockState turnClock)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Status != GameStatus.Started) throw new InvalidOperationException("Game not started.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.OwnerId != command.Caller.UserId.Value) throw new InvalidOperationException("You do not control this player.");
        if (state.CurrentPlayerId != command.PlayerId) throw new InvalidOperationException("Not your turn.");
        if (state.PendingGemReturnPlayerId is not null) throw new InvalidOperationException("A gem overflow resolution is pending.");
        if (state.PendingNobleSelectionPlayerId is not null) throw new InvalidOperationException("A noble selection is pending.");
        if (!turnClock.CanStartAction(command.PlayerId)) throw new InvalidOperationException("Turn deadline has passed.");
        SplendorRules.EnsureCanReserveCard(player.ReservedCardIds.Count);

        var isBlindReservation = command.CardId is null;
        var level = command.Level ?? 0;
        var cardId = command.CardId;

        if (isBlindReservation)
        {
            if (command.Level is null) throw new InvalidOperationException("Deck level is required.");
            var selectedDeck = state.DeckFor(level);
            if (selectedDeck.Count == 0) throw new InvalidOperationException("Deck is empty.");
            cardId = selectedDeck[0];
        }
        else
        {
            var card = Domain.CardDefinitions.GetById(cardId!) ?? throw new InvalidOperationException("Card not found.");
            level = card.Level;
            if (!state.MarketFor(level).Contains(cardId!))
            {
                throw new InvalidOperationException("Card not available in market.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var events = new List<IDomainEvent>();

        if (state.MarketGems.Gold > 0)
        {
            events.Add(new GemsTaken(command.GameId, command.PlayerId, new GemCollection(0, 0, 0, 0, 0, 1), now));
        }

        events.Add(new CardReserved(command.GameId, command.PlayerId, cardId!, now));

        var deck = state.DeckFor(level);
        if (!isBlindReservation && deck.Count > 0)
        {
            events.Add(new CardRevealed(command.GameId, level, deck[0], now));
        }

        return events;
    }
}
