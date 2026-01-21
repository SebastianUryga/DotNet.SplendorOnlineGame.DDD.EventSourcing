using Splendor.Domain.ValueObjects;

namespace Splendor.Domain;

public static class CardDefinitions
{
    // MVP: Subset of cards. Full game has 90 cards.
    public static readonly IReadOnlyList<Card> AllCards = new List<Card>
    {
        // Level 1 cards (40 in full game, 8 here for MVP)
        new("L1_01", 1, GemType.Diamond, 0, new GemCollection(0, 1, 1, 1, 1, 0)),
        new("L1_02", 1, GemType.Diamond, 0, new GemCollection(0, 2, 0, 0, 2, 0)),
        new("L1_03", 1, GemType.Sapphire, 0, new GemCollection(1, 0, 1, 1, 1, 0)),
        new("L1_04", 1, GemType.Sapphire, 0, new GemCollection(0, 0, 2, 0, 2, 0)),
        new("L1_05", 1, GemType.Emerald, 0, new GemCollection(1, 1, 0, 1, 1, 0)),
        new("L1_06", 1, GemType.Emerald, 0, new GemCollection(2, 0, 0, 2, 0, 0)),
        new("L1_07", 1, GemType.Ruby, 0, new GemCollection(1, 1, 1, 0, 1, 0)),
        new("L1_08", 1, GemType.Ruby, 0, new GemCollection(0, 2, 2, 0, 0, 0)),
        new("L1_09", 1, GemType.Onyx, 0, new GemCollection(1, 1, 1, 1, 0, 0)),
        new("L1_10", 1, GemType.Onyx, 0, new GemCollection(2, 0, 0, 0, 2, 0)),
        new("L1_11", 1, GemType.Diamond, 1, new GemCollection(0, 0, 4, 0, 0, 0)),
        new("L1_12", 1, GemType.Sapphire, 1, new GemCollection(0, 0, 0, 4, 0, 0)),

        // Level 2 cards (30 in full game, 6 here for MVP)
        new("L2_01", 2, GemType.Diamond, 1, new GemCollection(0, 2, 2, 3, 0, 0)),
        new("L2_02", 2, GemType.Diamond, 2, new GemCollection(0, 0, 0, 5, 0, 0)),
        new("L2_03", 2, GemType.Sapphire, 1, new GemCollection(2, 0, 2, 0, 3, 0)),
        new("L2_04", 2, GemType.Sapphire, 2, new GemCollection(0, 0, 0, 0, 5, 0)),
        new("L2_05", 2, GemType.Emerald, 1, new GemCollection(3, 2, 0, 2, 0, 0)),
        new("L2_06", 2, GemType.Emerald, 2, new GemCollection(0, 5, 0, 0, 0, 0)),
        new("L2_07", 2, GemType.Ruby, 1, new GemCollection(0, 3, 0, 2, 2, 0)),
        new("L2_08", 2, GemType.Ruby, 2, new GemCollection(5, 0, 0, 0, 0, 0)),

        // Level 3 cards (20 in full game, 4 here for MVP)
        new("L3_01", 3, GemType.Diamond, 3, new GemCollection(0, 3, 3, 5, 3, 0)),
        new("L3_02", 3, GemType.Diamond, 4, new GemCollection(0, 0, 0, 7, 0, 0)),
        new("L3_03", 3, GemType.Sapphire, 3, new GemCollection(3, 0, 3, 3, 5, 0)),
        new("L3_04", 3, GemType.Sapphire, 4, new GemCollection(0, 0, 0, 0, 7, 0)),
        new("L3_05", 3, GemType.Emerald, 3, new GemCollection(5, 3, 0, 3, 3, 0)),
        new("L3_06", 3, GemType.Emerald, 4, new GemCollection(0, 7, 0, 0, 0, 0)),
    };

    public static IReadOnlyList<Card> GetLevel(int level) =>
        AllCards.Where(c => c.Level == level).ToList();

    public static Card? GetById(string id) =>
        AllCards.FirstOrDefault(c => c.Id == id);
}
