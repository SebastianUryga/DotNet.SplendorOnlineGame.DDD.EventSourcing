using Marten;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public record ReserveCardCommand(Guid GameId, string OwnerId, string PlayerId, string CardId);

public class ReserveCardCommandHandler
{
    private readonly IDocumentSession _session;

    public ReserveCardCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(ReserveCardCommand command, CancellationToken cancellationToken)
    {
        var query = ReserveCardDecisionState.Query(command.GameId);
        var boundary = await _session.Events.FetchForWritingByTags<ReserveCardDecisionState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var afterReserveCardEvents = Decide(command, state);

        var completionState = TurnCompletionDecisionState.From(state);
        completionState.Apply(afterReserveCardEvents);

        var events = afterReserveCardEvents.Concat(TurnCompletion.Decide(command.GameId, command.PlayerId, completionState, DateTimeOffset.UtcNow));

        boundary.AppendMany(events.Select(e => _session.Tag(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IEnumerable<IDomainEvent> Decide(ReserveCardCommand command, ReserveCardDecisionState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (!state.Started) throw new InvalidOperationException("Game not started.");
        if (!state.Players.TryGetValue(command.PlayerId, out var player)) throw new InvalidOperationException("Player not found.");
        if (player.OwnerId != command.OwnerId) throw new InvalidOperationException("You do not control this player.");
        if (state.CurrentPlayerId != command.PlayerId) throw new InvalidOperationException("Not your turn.");
        if (state.PendingGemReturnPlayerId is not null) throw new InvalidOperationException("A gem overflow resolution is pending.");

        var card = CardCatalog.GetById(command.CardId) ?? throw new InvalidOperationException("Card not found.");
        var market = state.MarketFor(card);

        if (!market.Contains(command.CardId))
        {
            throw new InvalidOperationException("Card not available in market.");
        }

        var now = DateTimeOffset.UtcNow;
        var events = new List<IDomainEvent>
        {
            new CardReserved(command.GameId, command.PlayerId, command.CardId, now)
        };

        var deck = state.DeckFor(card.Level);
        if (market.Contains(command.CardId) && deck.Count > 0)
        {
            events.Add(new CardRevealed(command.GameId, card.Level, deck[0], now));
        }

        return events;
    }
}
