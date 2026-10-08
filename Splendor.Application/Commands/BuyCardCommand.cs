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

public record BuyCardCommand : IAuthorizedCommand, IRequest
{
    public Guid GameId { get; init; }
    public required Caller Caller { get; init; }
    public string PlayerId { get; init; } = string.Empty;
    public string CardId { get; init; } = string.Empty;
}

public class BuyCardCommandHandler : IRequestHandler<BuyCardCommand>
{
    private readonly IDocumentSession _session;

    public BuyCardCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(BuyCardCommand command, CancellationToken cancellationToken)
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
        stream.AppendMany(events.Select(e => _session.TagEvent(e, state.GameCreatorId)));
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<IDomainEvent> Decide(BuyCardCommand command, SplendorGameState state, TurnClockState turnClock)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Status != GameStatus.Started) throw new InvalidOperationException("Game not started.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.PlayerOwnerId != command.Caller.UserId.Value) throw new InvalidOperationException("You do not control this player.");
        if (state.CurrentPlayerId != command.PlayerId) throw new InvalidOperationException("Not your turn.");
        if (state.PendingGemReturnPlayerId is not null) throw new InvalidOperationException("A gem overflow resolution is pending.");
        if (state.PendingNobleSelectionPlayerId is not null) throw new InvalidOperationException("A noble selection is pending.");
        if (!turnClock.CanStartAction(command.PlayerId)) throw new InvalidOperationException("Turn deadline has passed.");

        var card = Splendor.Domain.CardDefinitions.GetById(command.CardId) ?? throw new InvalidOperationException("Card not found.");
        var market = state.MarketFor(card);
        var isReservedByPlayer = player.ReservedCardIds.Contains(command.CardId);

        if (!market.Contains(command.CardId) && !isReservedByPlayer)
        {
            throw new InvalidOperationException("Card not available in market or reserved by player.");
        }

        var effectiveCost = SplendorRules.CalculateEffectiveCost(card.Cost, SplendorRules.GetBonuses(player.OwnedCardIds));
        if (!SplendorRules.CanAfford(player.Gems, effectiveCost))
        {
            throw new InvalidOperationException("Cannot afford this card.");
        }

        var now = DateTimeOffset.UtcNow;
        var events = new List<IDomainEvent>();
        var payment = SplendorRules.CalculatePayment(player.Gems, effectiveCost);

        events.Add(new CardPurchased(command.GameId, command.PlayerId, command.CardId, payment, now));

        var deck = state.DeckFor(card.Level);
        if (market.Contains(command.CardId) && deck.Count > 0)
        {
            events.Add(new CardRevealed(command.GameId, card.Level, deck[0], now));
        }

        return events;
    }
}
