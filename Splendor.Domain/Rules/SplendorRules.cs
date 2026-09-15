using Splendor.Domain.ValueObjects;

namespace Splendor.Domain.Rules;

public static class SplendorRules
{
    private const int MaxReservedCards = 3;
    private const int MaxDifferentGemColorsTaken = 3;
    private const int SameColorGemsTaken = 2;
    private const int MinimumMarketGemsForSameColorTake = 4;

    public static GemCollection GetBonuses(IEnumerable<string> ownedCardIds)
    {
        var diamond = 0;
        var sapphire = 0;
        var emerald = 0;
        var ruby = 0;
        var onyx = 0;

        foreach (var cardId in ownedCardIds)
        {
            switch (CardDefinitions.GetById(cardId)?.BonusType)
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

    public static int GetPrestigePoints(IEnumerable<string> ownedCardIds, IEnumerable<string> ownedNobleIds) =>
        ownedCardIds.Sum(cardId => CardDefinitions.GetById(cardId)?.PrestigePoints ?? 0) +
        ownedNobleIds.Sum(nobleId => NobleDefinitions.GetById(nobleId)?.PrestigePoints ?? 0);

    public static bool MeetsNobleRequirements(GemCollection bonuses, GemCollection requirements) =>
        bonuses.Diamond >= requirements.Diamond &&
        bonuses.Sapphire >= requirements.Sapphire &&
        bonuses.Emerald >= requirements.Emerald &&
        bonuses.Ruby >= requirements.Ruby &&
        bonuses.Onyx >= requirements.Onyx;

    public static void EnsureCanReserveCard(int reservedCardCount)
    {
        if (reservedCardCount >= MaxReservedCards) throw new InvalidOperationException("Cannot reserve more than 3 cards.");
    }

    public static void EnsureValidGemSelection(GemCollection gems)
    {
        var colorCounts = new[] { gems.Diamond, gems.Sapphire, gems.Emerald, gems.Ruby, gems.Onyx };
        var nonZeroColors = colorCounts.Where(c => c > 0).ToList();

        var isOptionA = gems.Gold == 0 && nonZeroColors.All(c => c == 1) && nonZeroColors.Count is >= 1 and <= MaxDifferentGemColorsTaken;
        var isOptionB = gems.Gold == 0 && nonZeroColors.Count == 1 && nonZeroColors[0] == SameColorGemsTaken;
        var isOptionC = gems.Gold == 1 && nonZeroColors.Count == 0;

        if (!isOptionA && !isOptionB && !isOptionC)
        {
            throw new InvalidOperationException("Invalid gem selection.");
        }
    }

    public static void EnsureGemsAvailable(GemCollection marketGems, GemCollection gems)
    {
        if (gems.Diamond == SameColorGemsTaken && marketGems.Diamond < MinimumMarketGemsForSameColorTake) throw new InvalidOperationException("Not enough diamonds on market.");
        if (gems.Sapphire == SameColorGemsTaken && marketGems.Sapphire < MinimumMarketGemsForSameColorTake) throw new InvalidOperationException("Not enough sapphires on market.");
        if (gems.Emerald == SameColorGemsTaken && marketGems.Emerald < MinimumMarketGemsForSameColorTake) throw new InvalidOperationException("Not enough emeralds on market.");
        if (gems.Ruby == SameColorGemsTaken && marketGems.Ruby < MinimumMarketGemsForSameColorTake) throw new InvalidOperationException("Not enough rubies on market.");
        if (gems.Onyx == SameColorGemsTaken && marketGems.Onyx < MinimumMarketGemsForSameColorTake) throw new InvalidOperationException("Not enough onyxes on market.");

        if (marketGems.Diamond < gems.Diamond) throw new InvalidOperationException("Not enough diamonds on market.");
        if (marketGems.Sapphire < gems.Sapphire) throw new InvalidOperationException("Not enough sapphires on market.");
        if (marketGems.Emerald < gems.Emerald) throw new InvalidOperationException("Not enough emeralds on market.");
        if (marketGems.Ruby < gems.Ruby) throw new InvalidOperationException("Not enough rubies on market.");
        if (marketGems.Onyx < gems.Onyx) throw new InvalidOperationException("Not enough onyxes on market.");
        if (marketGems.Gold < gems.Gold) throw new InvalidOperationException("Not enough gold on market.");
    }

    public static GemCollection CalculateEffectiveCost(GemCollection cost, GemCollection bonuses) =>
        new(
            Math.Max(0, cost.Diamond - bonuses.Diamond),
            Math.Max(0, cost.Sapphire - bonuses.Sapphire),
            Math.Max(0, cost.Emerald - bonuses.Emerald),
            Math.Max(0, cost.Ruby - bonuses.Ruby),
            Math.Max(0, cost.Onyx - bonuses.Onyx),
            0);

    public static bool CanAfford(GemCollection playerGems, GemCollection cost)
    {
        var deficit = 0;
        deficit += Math.Max(0, cost.Diamond - playerGems.Diamond);
        deficit += Math.Max(0, cost.Sapphire - playerGems.Sapphire);
        deficit += Math.Max(0, cost.Emerald - playerGems.Emerald);
        deficit += Math.Max(0, cost.Ruby - playerGems.Ruby);
        deficit += Math.Max(0, cost.Onyx - playerGems.Onyx);
        return deficit <= playerGems.Gold;
    }

    public static GemCollection CalculatePayment(GemCollection playerGems, GemCollection cost)
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

    public static PlayerScoreCandidate SelectWinner(IEnumerable<PlayerScoreCandidate> players) =>
        players
            .OrderByDescending(player => player.PrestigePoints)
            .ThenBy(player => player.PurchasedCardCount)
            .ThenBy(player => player.PlayerOrder)
            .First();
}

public record PlayerScoreCandidate(
    string PlayerId,
    string OwnerId,
    string Name,
    int PrestigePoints,
    int PurchasedCardCount,
    int PlayerOrder);
