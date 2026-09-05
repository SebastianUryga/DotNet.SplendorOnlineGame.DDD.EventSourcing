using Splendor.Domain.ValueObjects;

namespace Splendor.BotWorker.Strategies;

public class GreedyBotStrategy : IBotStrategy
{
    public BotAction? ChooseMove(GameView game, string botPlayerId, IReadOnlyDictionary<string, Card> cards)
    {
        var me = game.Players.Single(p => p.Id == botPlayerId);

        if (game.PlayerIdAwaitingNobleSelection is not null && game.EligibleNobleIds is not null)
            return new ChooseNobleAction(game.EligibleNobleIds.First());

        var bonuses = GetPlayerBonuses(me.OwnedCardIds.Select(x => cards[x]).ToList());

        var priorityCards = me.ReservedCardIds
            .Concat(game.Market3)
            .Concat(game.Market2)
            .Concat(game.Market1);

        foreach (var cardId in priorityCards)
        {
            var card = cards[cardId];

            if (me.Gems is not null && CanAfford(card, me.Gems + bonuses))
            {
                return new BuyCardAction(card.Id);
            }
        }


        if (me.Gems is not null && me.Gems.Total >= 8 && me.ReservedCardIds.Count < 3)
        {
            var cardToReserveId = game.Market3
                .Concat(game.Market2)
                .Concat(game.Market1)
                .FirstOrDefault();

            if (cardToReserveId != null)
            {
                return new ReserveCardAction(cardToReserveId);
            }
        }

        var colors = new[]
        {
            game.MarketGems.Diamond,
            game.MarketGems.Sapphire,
            game.MarketGems.Emerald,
            game.MarketGems.Ruby,
            game.MarketGems.Onyx
        };

        var availableIndexes = colors
            .Select((count, index) => new { count, index })
            .Where(x => x.count > 0)
            .Take(3)
            .Select(x => x.index)
            .ToHashSet();

        if (availableIndexes.Count == 0)
        {
            return null;
        }

        return new TakeGemsAction(
            Diamond: availableIndexes.Contains(0) ? 1 : 0,
            Sapphire: availableIndexes.Contains(1) ? 1 : 0,
            Emerald: availableIndexes.Contains(2) ? 1 : 0,
            Ruby: availableIndexes.Contains(3) ? 1 : 0,
            Onyx: availableIndexes.Contains(4) ? 1 : 0,
            Gold: 0);

    }


    private bool CanAfford(Card card, GemCollection gems)
    {
        int missingDiamond = Math.Max(0, card.Cost.Diamond - gems.Diamond);
        int missingSapphire = Math.Max(0, card.Cost.Sapphire - gems.Sapphire);
        int missingEmerald = Math.Max(0, card.Cost.Emerald - gems.Emerald);
        int missingRuby = Math.Max(0, card.Cost.Ruby - gems.Ruby);
        int missingOnyx = Math.Max(0, card.Cost.Onyx - gems.Onyx);

        int totalMissing = missingDiamond + missingSapphire + missingEmerald + missingRuby + missingOnyx;

        return totalMissing <= gems.Gold;
    }

    private GemCollection GetPlayerBonuses(List<Card> cards) => new GemCollection(
            cards.Count(c => c.BonusType == GemType.Diamond),
            cards.Count(c => c.BonusType == GemType.Sapphire),
            cards.Count(c => c.BonusType == GemType.Emerald),
            cards.Count(c => c.BonusType == GemType.Ruby),
            cards.Count(c => c.BonusType == GemType.Onyx),
            Gold: 0
        );
}