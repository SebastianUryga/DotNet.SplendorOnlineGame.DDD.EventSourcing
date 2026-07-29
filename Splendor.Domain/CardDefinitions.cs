using Splendor.Domain.ValueObjects;

namespace Splendor.Domain;

public static class CardDefinitions
{
    // Full game has 90 cards.
    public static readonly IReadOnlyList<Card> AllCards = new List<Card>
    {
        // Level 1 cards (40)
        new("L1_01", 1, GemType.Diamond, 0, new GemCollection(0, 1, 1, 1, 1, 0)),
        new("L1_02", 1, GemType.Diamond, 0, new GemCollection(0, 2, 0, 0, 2, 0)),
        new("L1_03", 1, GemType.Diamond, 0, new GemCollection(0, 3, 0, 0, 0, 0)),
        new("L1_04", 1, GemType.Diamond, 0, new GemCollection(0, 0, 2, 2, 0, 0)),
        new("L1_05", 1, GemType.Diamond, 0, new GemCollection(0, 2, 2, 1, 0, 0)),
        new("L1_06", 1, GemType.Diamond, 0, new GemCollection(0, 1, 2, 1, 1, 0)),
        new("L1_07", 1, GemType.Diamond, 1, new GemCollection(0, 0, 4, 0, 0, 0)),
        new("L1_08", 1, GemType.Diamond, 0, new GemCollection(3, 0, 0, 0, 1, 0)),
        new("L1_09", 1, GemType.Sapphire, 0, new GemCollection(1, 0, 1, 1, 1, 0)),
        new("L1_10", 1, GemType.Sapphire, 0, new GemCollection(0, 0, 2, 0, 2, 0)),
        new("L1_11", 1, GemType.Sapphire, 0, new GemCollection(0, 0, 3, 0, 0, 0)),
        new("L1_12", 1, GemType.Sapphire, 0, new GemCollection(2, 0, 0, 2, 0, 0)),
        new("L1_13", 1, GemType.Sapphire, 0, new GemCollection(2, 0, 1, 1, 1, 0)),
        new("L1_14", 1, GemType.Sapphire, 0, new GemCollection(1, 0, 2, 2, 0, 0)),
        new("L1_15", 1, GemType.Sapphire, 1, new GemCollection(0, 0, 0, 4, 0, 0)),
        new("L1_16", 1, GemType.Sapphire, 0, new GemCollection(0, 0, 1, 3, 1, 0)),
        new("L1_17", 1, GemType.Emerald, 0, new GemCollection(1, 1, 0, 1, 1, 0)),
        new("L1_18", 1, GemType.Emerald, 0, new GemCollection(2, 0, 0, 2, 0, 0)),
        new("L1_19", 1, GemType.Emerald, 0, new GemCollection(0, 0, 0, 3, 0, 0)),
        new("L1_20", 1, GemType.Emerald, 0, new GemCollection(0, 2, 0, 0, 2, 0)),
        new("L1_21", 1, GemType.Emerald, 0, new GemCollection(1, 2, 0, 1, 1, 0)),
        new("L1_22", 1, GemType.Emerald, 0, new GemCollection(2, 1, 0, 2, 0, 0)),
        new("L1_23", 1, GemType.Emerald, 1, new GemCollection(4, 0, 0, 0, 0, 0)),
        new("L1_24", 1, GemType.Emerald, 0, new GemCollection(1, 0, 0, 1, 3, 0)),
        new("L1_25", 1, GemType.Ruby, 0, new GemCollection(1, 1, 1, 0, 1, 0)),
        new("L1_26", 1, GemType.Ruby, 0, new GemCollection(0, 2, 2, 0, 0, 0)),
        new("L1_27", 1, GemType.Ruby, 0, new GemCollection(0, 0, 0, 0, 3, 0)),
        new("L1_28", 1, GemType.Ruby, 0, new GemCollection(2, 2, 0, 0, 0, 0)),
        new("L1_29", 1, GemType.Ruby, 0, new GemCollection(1, 1, 2, 0, 1, 0)),
        new("L1_30", 1, GemType.Ruby, 0, new GemCollection(0, 2, 1, 0, 2, 0)),
        new("L1_31", 1, GemType.Ruby, 1, new GemCollection(0, 4, 0, 0, 0, 0)),
        new("L1_32", 1, GemType.Ruby, 0, new GemCollection(3, 1, 1, 0, 0, 0)),
        new("L1_33", 1, GemType.Onyx, 0, new GemCollection(1, 1, 1, 1, 0, 0)),
        new("L1_34", 1, GemType.Onyx, 0, new GemCollection(2, 0, 0, 0, 2, 0)),
        new("L1_35", 1, GemType.Onyx, 0, new GemCollection(3, 0, 0, 0, 0, 0)),
        new("L1_36", 1, GemType.Onyx, 0, new GemCollection(0, 0, 2, 2, 0, 0)),
        new("L1_37", 1, GemType.Onyx, 0, new GemCollection(1, 1, 1, 2, 0, 0)),
        new("L1_38", 1, GemType.Onyx, 0, new GemCollection(2, 0, 2, 1, 0, 0)),
        new("L1_39", 1, GemType.Onyx, 1, new GemCollection(0, 0, 0, 0, 4, 0)),
        new("L1_40", 1, GemType.Onyx, 0, new GemCollection(0, 3, 1, 1, 0, 0)),

        // Level 2 cards (30)
        new("L2_01", 2, GemType.Diamond, 1, new GemCollection(0, 2, 2, 3, 0, 0)),
        new("L2_02", 2, GemType.Diamond, 2, new GemCollection(0, 0, 0, 5, 0, 0)),
        new("L2_03", 2, GemType.Diamond, 2, new GemCollection(0, 4, 2, 1, 0, 0)),
        new("L2_04", 2, GemType.Diamond, 2, new GemCollection(3, 0, 0, 0, 5, 0)),
        new("L2_05", 2, GemType.Diamond, 2, new GemCollection(0, 3, 0, 0, 5, 0)),
        new("L2_06", 2, GemType.Diamond, 1, new GemCollection(2, 0, 0, 2, 4, 0)),
        new("L2_07", 2, GemType.Sapphire, 1, new GemCollection(2, 0, 2, 0, 3, 0)),
        new("L2_08", 2, GemType.Sapphire, 2, new GemCollection(0, 0, 0, 0, 5, 0)),
        new("L2_09", 2, GemType.Sapphire, 2, new GemCollection(2, 0, 4, 1, 0, 0)),
        new("L2_10", 2, GemType.Sapphire, 2, new GemCollection(0, 3, 5, 0, 0, 0)),
        new("L2_11", 2, GemType.Sapphire, 2, new GemCollection(5, 0, 3, 0, 0, 0)),
        new("L2_12", 2, GemType.Sapphire, 1, new GemCollection(4, 2, 0, 0, 2, 0)),
        new("L2_13", 2, GemType.Emerald, 1, new GemCollection(3, 2, 0, 2, 0, 0)),
        new("L2_14", 2, GemType.Emerald, 2, new GemCollection(0, 5, 0, 0, 0, 0)),
        new("L2_15", 2, GemType.Emerald, 2, new GemCollection(1, 0, 0, 4, 2, 0)),
        new("L2_16", 2, GemType.Emerald, 2, new GemCollection(0, 0, 3, 5, 0, 0)),
        new("L2_17", 2, GemType.Emerald, 2, new GemCollection(0, 5, 0, 3, 0, 0)),
        new("L2_18", 2, GemType.Emerald, 1, new GemCollection(0, 4, 2, 0, 2, 0)),
        new("L2_19", 2, GemType.Ruby, 1, new GemCollection(0, 3, 0, 2, 2, 0)),
        new("L2_20", 2, GemType.Ruby, 2, new GemCollection(5, 0, 0, 0, 0, 0)),
        new("L2_21", 2, GemType.Ruby, 2, new GemCollection(4, 1, 2, 0, 0, 0)),
        new("L2_22", 2, GemType.Ruby, 2, new GemCollection(0, 0, 0, 3, 5, 0)),
        new("L2_23", 2, GemType.Ruby, 2, new GemCollection(5, 0, 0, 0, 3, 0)),
        new("L2_24", 2, GemType.Ruby, 1, new GemCollection(2, 0, 4, 2, 0, 0)),
        new("L2_25", 2, GemType.Onyx, 1, new GemCollection(3, 0, 3, 0, 2, 0)),
        new("L2_26", 2, GemType.Onyx, 2, new GemCollection(0, 0, 5, 0, 0, 0)),
        new("L2_27", 2, GemType.Onyx, 2, new GemCollection(0, 2, 1, 4, 0, 0)),
        new("L2_28", 2, GemType.Onyx, 2, new GemCollection(5, 3, 0, 0, 0, 0)),
        new("L2_29", 2, GemType.Onyx, 2, new GemCollection(0, 0, 5, 3, 0, 0)),
        new("L2_30", 2, GemType.Onyx, 1, new GemCollection(2, 2, 0, 4, 0, 0)),

        // Level 3 cards (20)
        new("L3_01", 3, GemType.Diamond, 3, new GemCollection(0, 3, 3, 5, 3, 0)),
        new("L3_02", 3, GemType.Diamond, 4, new GemCollection(0, 0, 0, 7, 0, 0)),
        new("L3_03", 3, GemType.Diamond, 4, new GemCollection(3, 0, 0, 0, 7, 0)),
        new("L3_04", 3, GemType.Diamond, 4, new GemCollection(3, 0, 0, 3, 6, 0)),
        new("L3_05", 3, GemType.Sapphire, 3, new GemCollection(3, 0, 3, 3, 5, 0)),
        new("L3_06", 3, GemType.Sapphire, 4, new GemCollection(0, 0, 0, 0, 7, 0)),
        new("L3_07", 3, GemType.Sapphire, 4, new GemCollection(7, 3, 0, 0, 0, 0)),
        new("L3_08", 3, GemType.Sapphire, 4, new GemCollection(6, 3, 0, 0, 3, 0)),
        new("L3_09", 3, GemType.Emerald, 3, new GemCollection(5, 3, 0, 3, 3, 0)),
        new("L3_10", 3, GemType.Emerald, 4, new GemCollection(0, 7, 0, 0, 0, 0)),
        new("L3_11", 3, GemType.Emerald, 4, new GemCollection(0, 0, 7, 3, 0, 0)),
        new("L3_12", 3, GemType.Emerald, 4, new GemCollection(3, 6, 3, 0, 0, 0)),
        new("L3_13", 3, GemType.Ruby, 3, new GemCollection(3, 5, 3, 0, 3, 0)),
        new("L3_14", 3, GemType.Ruby, 4, new GemCollection(7, 0, 0, 0, 0, 0)),
        new("L3_15", 3, GemType.Ruby, 4, new GemCollection(0, 0, 0, 7, 3, 0)),
        new("L3_16", 3, GemType.Ruby, 4, new GemCollection(0, 3, 6, 3, 0, 0)),
        new("L3_17", 3, GemType.Onyx, 3, new GemCollection(3, 3, 5, 3, 0, 0)),
        new("L3_18", 3, GemType.Onyx, 4, new GemCollection(0, 0, 7, 0, 0, 0)),
        new("L3_19", 3, GemType.Onyx, 4, new GemCollection(0, 7, 0, 0, 3, 0)),
        new("L3_20", 3, GemType.Onyx, 4, new GemCollection(0, 0, 3, 6, 3, 0))
    };

    public static IReadOnlyList<Card> GetLevel(int level) =>
        AllCards.Where(c => c.Level == level).ToList();

    public static Card? GetById(string id) =>
        AllCards.FirstOrDefault(c => c.Id == id);
}
