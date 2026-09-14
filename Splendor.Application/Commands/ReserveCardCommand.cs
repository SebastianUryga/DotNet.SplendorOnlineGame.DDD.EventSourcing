using Marten;
using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.DecisionStates;
using Splendor.Application.Events;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;

namespace Splendor.Application.Commands;

public record ReserveCardCommand : IAuthoredCommand, IRequest
{
    public Guid GameId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string PlayerId { get; init; } = string.Empty;
    public string CardId { get; init; } = string.Empty;
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
        var query = SplendorGameState.Query(command.GameId);
        var boundary = await _session.Events.FetchForWritingByTags<SplendorGameState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state).ToList();

        // Apply decision events to local state
        state.Apply(events);

        // Turn completion may produce additional events; merge them
        var completionEvents = TurnCompletion.Decide(command.GameId, command.PlayerId, state, DateTimeOffset.UtcNow);
        events.AddRange(completionEvents);

        // Tag and append events to the boundary
        boundary.AppendMany(events.Select(e => _session.TagEvent(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }



    internal static IReadOnlyList<IDomainEvent> Decide(ReserveCardCommand command, SplendorGameState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Status != GameStatus.Started) throw new InvalidOperationException("Game not started.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.OwnerId != command.OwnerId) throw new InvalidOperationException("You do not control this player.");
        if (state.CurrentPlayerId != command.PlayerId) throw new InvalidOperationException("Not your turn.");
        if (state.PendingGemReturnPlayerId is not null) throw new InvalidOperationException("A gem overflow resolution is pending.");
        if (state.PendingNobleSelectionPlayerId is not null) throw new InvalidOperationException("A noble selection is pending.");

        var card = Domain.CardDefinitions.GetById(command.CardId) ?? throw new InvalidOperationException("Card not found.");
        var market = state.MarketFor(card);

        if (!market.Contains(command.CardId))
        {
            throw new InvalidOperationException("Card not available in market.");
        }

        var now = DateTimeOffset.UtcNow;
        var events = new List<IDomainEvent>();

        if (state.MarketGems.Gold > 0)
        {
            events.Add(new GemsTaken(command.GameId, command.PlayerId, new GemCollection(0, 0, 0, 0, 0, 1), now));
        }

        events.Add(new CardReserved(command.GameId, command.PlayerId, command.CardId, now));

        var deck = state.DeckFor(card.Level);
        if (market.Contains(command.CardId) && deck.Count > 0)
        {
            events.Add(new CardRevealed(command.GameId, card.Level, deck[0], now));
        }

        return events;
    }
}
