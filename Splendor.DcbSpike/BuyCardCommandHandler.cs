using Marten;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public record BuyCardCommand(Guid GameId, string OwnerId, string PlayerId, string CardId);

public class BuyCardCommandHandler
{
    private readonly IDocumentSession _session;

    public BuyCardCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(BuyCardCommand command, CancellationToken cancellationToken)
    {
        var query = BuyCardDecisionState.Query(command.GameId);
        var boundary = await _session.Events.FetchForWritingByTags<BuyCardDecisionState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state);

        boundary.AppendMany(events.Select(e => _session.Tag(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<IDomainEvent> Decide(BuyCardCommand command, BuyCardDecisionState state)
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
        var isReservedByPlayer = player.ReservedCardIds.Contains(command.CardId);

        if (!market.Contains(command.CardId) && !isReservedByPlayer)
        {
            throw new InvalidOperationException("Card not available in market or reserved by player.");
        }

        var effectiveCost = CalculateEffectiveCost(card.Cost, GetPlayerBonuses(player));
        if (!CanAfford(player.Gems, effectiveCost))
        {
            throw new InvalidOperationException("Cannot afford this card.");
        }

        var now = DateTimeOffset.UtcNow;
        var events = new List<IDomainEvent>();
        var payment = CalculatePayment(player.Gems, effectiveCost);

        events.Add(new CardPurchased(command.GameId, command.PlayerId, command.CardId, payment, now));

        var deck = state.DeckFor(card.Level);
        if (market.Contains(command.CardId) && deck.Count > 0)
        {
            events.Add(new CardRevealed(command.GameId, card.Level, deck[0], now));
        }

        var completionState = TurnCompletionDecisionState.From(state);
        completionState.Apply(events);
        events.AddRange(TurnCompletion.Decide(command.GameId, command.PlayerId, completionState, now));
        return events;
    }

    private static GemCollection CalculateEffectiveCost(GemCollection cost, GemCollection bonuses) =>
        new(
            Math.Max(0, cost.Diamond - bonuses.Diamond),
            Math.Max(0, cost.Sapphire - bonuses.Sapphire),
            Math.Max(0, cost.Emerald - bonuses.Emerald),
            Math.Max(0, cost.Ruby - bonuses.Ruby),
            Math.Max(0, cost.Onyx - bonuses.Onyx),
            0);

    private static bool CanAfford(GemCollection playerGems, GemCollection cost)
    {
        var deficit = 0;
        deficit += Math.Max(0, cost.Diamond - playerGems.Diamond);
        deficit += Math.Max(0, cost.Sapphire - playerGems.Sapphire);
        deficit += Math.Max(0, cost.Emerald - playerGems.Emerald);
        deficit += Math.Max(0, cost.Ruby - playerGems.Ruby);
        deficit += Math.Max(0, cost.Onyx - playerGems.Onyx);
        return deficit <= playerGems.Gold;
    }

    private static GemCollection CalculatePayment(GemCollection playerGems, GemCollection cost)
    {
        var goldNeeded = 0;
        var dPay = Math.Min(playerGems.Diamond, cost.Diamond);
        goldNeeded += cost.Diamond - dPay;
        var sPay = Math.Min(playerGems.Sapphire, cost.Sapphire);
        goldNeeded += cost.Sapphire - sPay;
        var ePay = Math.Min(playerGems.Emerald, cost.Emerald);
        goldNeeded += cost.Emerald - ePay;
        var rPay = Math.Min(playerGems.Ruby, cost.Ruby);
        goldNeeded += cost.Ruby - rPay;
        var oPay = Math.Min(playerGems.Onyx, cost.Onyx);
        goldNeeded += cost.Onyx - oPay;

        return new GemCollection(dPay, sPay, ePay, rPay, oPay, goldNeeded);
    }

    private static GemCollection GetPlayerBonuses(PlayerState player)
    {
        var diamond = 0;
        var sapphire = 0;
        var emerald = 0;
        var ruby = 0;
        var onyx = 0;

        foreach (var cardId in player.OwnedCardIds)
        {
            switch (CardCatalog.GetById(cardId)?.BonusType)
            {
                case GemType.Diamond: diamond++; break;
                case GemType.Sapphire: sapphire++; break;
                case GemType.Emerald: emerald++; break;
                case GemType.Ruby: ruby++; break;
                case GemType.Onyx: onyx++; break;
            }
        }

        return new GemCollection(diamond, sapphire, emerald, ruby, onyx, 0);
    }
}
