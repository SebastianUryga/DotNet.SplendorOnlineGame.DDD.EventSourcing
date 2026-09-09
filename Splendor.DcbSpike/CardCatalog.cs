using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public static class CardCatalog
{
    // Full game has 90 cards.
    private static readonly Dictionary<string, Card> Cards = new(StringComparer.OrdinalIgnoreCase)
    {
        // Level 1 cards (40)
        ["L1_01"] = new("L1_01", 1, GemType.Diamond, 0, new GemCollection(0, 1, 1, 1, 1, 0)),
        ["L1_02"] = new("L1_02", 1, GemType.Diamond, 0, new GemCollection(0, 2, 0, 0, 2, 0)),
        ["L1_03"] = new("L1_03", 1, GemType.Diamond, 0, new GemCollection(0, 3, 0, 0, 0, 0)),
        ["L1_04"] = new("L1_04", 1, GemType.Diamond, 0, new GemCollection(0, 0, 2, 2, 0, 0)),
        ["L1_05"] = new("L1_05", 1, GemType.Diamond, 0, new GemCollection(0, 2, 2, 1, 0, 0)),
        ["L1_06"] = new("L1_06", 1, GemType.Diamond, 0, new GemCollection(0, 1, 2, 1, 1, 0)),
        ["L1_07"] = new("L1_07", 1, GemType.Diamond, 1, new GemCollection(0, 0, 4, 0, 0, 0)),
        ["L1_08"] = new("L1_08", 1, GemType.Diamond, 0, new GemCollection(3, 0, 0, 0, 1, 0)),
        ["L1_09"] = new("L1_09", 1, GemType.Sapphire, 0, new GemCollection(1, 0, 1, 1, 1, 0)),
        ["L1_10"] = new("L1_10", 1, GemType.Sapphire, 0, new GemCollection(0, 0, 2, 0, 2, 0)),
        ["L1_11"] = new("L1_11", 1, GemType.Sapphire, 0, new GemCollection(0, 0, 3, 0, 0, 0)),
        ["L1_12"] = new("L1_12", 1, GemType.Sapphire, 0, new GemCollection(2, 0, 0, 2, 0, 0)),
        ["L1_13"] = new("L1_13", 1, GemType.Sapphire, 0, new GemCollection(2, 0, 1, 1, 1, 0)),
        ["L1_14"] = new("L1_14", 1, GemType.Sapphire, 0, new GemCollection(1, 0, 2, 2, 0, 0)),
        ["L1_15"] = new("L1_15", 1, GemType.Sapphire, 1, new GemCollection(0, 0, 0, 4, 0, 0)),
        ["L1_16"] = new("L1_16", 1, GemType.Sapphire, 0, new GemCollection(0, 0, 1, 3, 1, 0)),
        ["L1_17"] = new("L1_17", 1, GemType.Emerald, 0, new GemCollection(1, 1, 0, 1, 1, 0)),
        ["L1_18"] = new("L1_18", 1, GemType.Emerald, 0, new GemCollection(2, 0, 0, 2, 0, 0)),
        ["L1_19"] = new("L1_19", 1, GemType.Emerald, 0, new GemCollection(0, 0, 0, 3, 0, 0)),
        ["L1_20"] = new("L1_20", 1, GemType.Emerald, 0, new GemCollection(0, 2, 0, 0, 2, 0)),
        ["L1_21"] = new("L1_21", 1, GemType.Emerald, 0, new GemCollection(1, 2, 0, 1, 1, 0)),
        ["L1_22"] = new("L1_22", 1, GemType.Emerald, 0, new GemCollection(2, 1, 0, 2, 0, 0)),
        ["L1_23"] = new("L1_23", 1, GemType.Emerald, 1, new GemCollection(4, 0, 0, 0, 0, 0)),
        ["L1_24"] = new("L1_24", 1, GemType.Emerald, 0, new GemCollection(1, 0, 0, 1, 3, 0)),
        ["L1_25"] = new("L1_25", 1, GemType.Ruby, 0, new GemCollection(1, 1, 1, 0, 1, 0)),
        ["L1_26"] = new("L1_26", 1, GemType.Ruby, 0, new GemCollection(0, 2, 2, 0, 0, 0)),
        ["L1_27"] = new("L1_27", 1, GemType.Ruby, 0, new GemCollection(0, 0, 0, 0, 3, 0)),
        ["L1_28"] = new("L1_28", 1, GemType.Ruby, 0, new GemCollection(2, 2, 0, 0, 0, 0)),
        ["L1_29"] = new("L1_29", 1, GemType.Ruby, 0, new GemCollection(1, 1, 2, 0, 1, 0)),
        ["L1_30"] = new("L1_30", 1, GemType.Ruby, 0, new GemCollection(0, 2, 1, 0, 2, 0)),
        ["L1_31"] = new("L1_31", 1, GemType.Ruby, 1, new GemCollection(0, 4, 0, 0, 0, 0)),
        ["L1_32"] = new("L1_32", 1, GemType.Ruby, 0, new GemCollection(3, 1, 1, 0, 0, 0)),
        ["L1_33"] = new("L1_33", 1, GemType.Onyx, 0, new GemCollection(1, 1, 1, 1, 0, 0)),
        ["L1_34"] = new("L1_34", 1, GemType.Onyx, 0, new GemCollection(2, 0, 0, 0, 2, 0)),
        ["L1_35"] = new("L1_35", 1, GemType.Onyx, 0, new GemCollection(3, 0, 0, 0, 0, 0)),
        ["L1_36"] = new("L1_36", 1, GemType.Onyx, 0, new GemCollection(0, 0, 2, 2, 0, 0)),
        ["L1_37"] = new("L1_37", 1, GemType.Onyx, 0, new GemCollection(1, 1, 1, 2, 0, 0)),
        ["L1_38"] = new("L1_38", 1, GemType.Onyx, 0, new GemCollection(2, 0, 2, 1, 0, 0)),
        ["L1_39"] = new("L1_39", 1, GemType.Onyx, 1, new GemCollection(0, 0, 0, 0, 4, 0)),
        ["L1_40"] = new("L1_40", 1, GemType.Onyx, 0, new GemCollection(0, 3, 1, 1, 0, 0)),

        // Level 2 cards (30)
        ["L2_01"] = new("L2_01", 2, GemType.Diamond, 1, new GemCollection(0, 2, 2, 3, 0, 0)),
        ["L2_02"] = new("L2_02", 2, GemType.Diamond, 2, new GemCollection(0, 0, 0, 5, 0, 0)),
        ["L2_03"] = new("L2_03", 2, GemType.Diamond, 2, new GemCollection(0, 4, 2, 1, 0, 0)),
        ["L2_04"] = new("L2_04", 2, GemType.Diamond, 2, new GemCollection(3, 0, 0, 0, 5, 0)),
        ["L2_05"] = new("L2_05", 2, GemType.Diamond, 2, new GemCollection(0, 3, 0, 0, 5, 0)),
        ["L2_06"] = new("L2_06", 2, GemType.Diamond, 1, new GemCollection(2, 0, 0, 2, 4, 0)),
        ["L2_07"] = new("L2_07", 2, GemType.Sapphire, 1, new GemCollection(2, 0, 2, 0, 3, 0)),
        ["L2_08"] = new("L2_08", 2, GemType.Sapphire, 2, new GemCollection(0, 0, 0, 0, 5, 0)),
        ["L2_09"] = new("L2_09", 2, GemType.Sapphire, 2, new GemCollection(2, 0, 4, 1, 0, 0)),
        ["L2_10"] = new("L2_10", 2, GemType.Sapphire, 2, new GemCollection(0, 3, 5, 0, 0, 0)),
        ["L2_11"] = new("L2_11", 2, GemType.Sapphire, 2, new GemCollection(5, 0, 3, 0, 0, 0)),
        ["L2_12"] = new("L2_12", 2, GemType.Sapphire, 1, new GemCollection(4, 2, 0, 0, 2, 0)),
        ["L2_13"] = new("L2_13", 2, GemType.Emerald, 1, new GemCollection(3, 2, 0, 2, 0, 0)),
        ["L2_14"] = new("L2_14", 2, GemType.Emerald, 2, new GemCollection(0, 5, 0, 0, 0, 0)),
        ["L2_15"] = new("L2_15", 2, GemType.Emerald, 2, new GemCollection(1, 0, 0, 4, 2, 0)),
        ["L2_16"] = new("L2_16", 2, GemType.Emerald, 2, new GemCollection(0, 0, 3, 5, 0, 0)),
        ["L2_17"] = new("L2_17", 2, GemType.Emerald, 2, new GemCollection(0, 5, 0, 3, 0, 0)),
        ["L2_18"] = new("L2_18", 2, GemType.Emerald, 1, new GemCollection(0, 4, 2, 0, 2, 0)),
        ["L2_19"] = new("L2_19", 2, GemType.Ruby, 1, new GemCollection(0, 3, 0, 2, 2, 0)),
        ["L2_20"] = new("L2_20", 2, GemType.Ruby, 2, new GemCollection(5, 0, 0, 0, 0, 0)),
        ["L2_21"] = new("L2_21", 2, GemType.Ruby, 2, new GemCollection(4, 1, 2, 0, 0, 0)),
        ["L2_22"] = new("L2_22", 2, GemType.Ruby, 2, new GemCollection(0, 0, 0, 3, 5, 0)),
        ["L2_23"] = new("L2_23", 2, GemType.Ruby, 2, new GemCollection(5, 0, 0, 0, 3, 0)),
        ["L2_24"] = new("L2_24", 2, GemType.Ruby, 1, new GemCollection(2, 0, 4, 2, 0, 0)),
        ["L2_25"] = new("L2_25", 2, GemType.Onyx, 1, new GemCollection(3, 0, 3, 0, 2, 0)),
        ["L2_26"] = new("L2_26", 2, GemType.Onyx, 2, new GemCollection(0, 0, 5, 0, 0, 0)),
        ["L2_27"] = new("L2_27", 2, GemType.Onyx, 2, new GemCollection(0, 2, 1, 4, 0, 0)),
        ["L2_28"] = new("L2_28", 2, GemType.Onyx, 2, new GemCollection(5, 3, 0, 0, 0, 0)),
        ["L2_29"] = new("L2_29", 2, GemType.Onyx, 2, new GemCollection(0, 0, 5, 3, 0, 0)),
        ["L2_30"] = new("L2_30", 2, GemType.Onyx, 1, new GemCollection(2, 2, 0, 4, 0, 0)),

        // Level 3 cards (20)
        ["L3_01"] = new("L3_01", 3, GemType.Diamond, 3, new GemCollection(0, 3, 3, 5, 3, 0)),
        ["L3_02"] = new("L3_02", 3, GemType.Diamond, 4, new GemCollection(0, 0, 0, 7, 0, 0)),
        ["L3_03"] = new("L3_03", 3, GemType.Diamond, 4, new GemCollection(3, 0, 0, 0, 7, 0)),
        ["L3_04"] = new("L3_04", 3, GemType.Diamond, 4, new GemCollection(3, 0, 0, 3, 6, 0)),
        ["L3_05"] = new("L3_05", 3, GemType.Sapphire, 3, new GemCollection(3, 0, 3, 3, 5, 0)),
        ["L3_06"] = new("L3_06", 3, GemType.Sapphire, 4, new GemCollection(0, 0, 0, 0, 7, 0)),
        ["L3_07"] = new("L3_07", 3, GemType.Sapphire, 4, new GemCollection(7, 3, 0, 0, 0, 0)),
        ["L3_08"] = new("L3_08", 3, GemType.Sapphire, 4, new GemCollection(6, 3, 0, 0, 3, 0)),
        ["L3_09"] = new("L3_09", 3, GemType.Emerald, 3, new GemCollection(5, 3, 0, 3, 3, 0)),
        ["L3_10"] = new("L3_10", 3, GemType.Emerald, 4, new GemCollection(0, 7, 0, 0, 0, 0)),
        ["L3_11"] = new("L3_11", 3, GemType.Emerald, 4, new GemCollection(0, 0, 7, 3, 0, 0)),
        ["L3_12"] = new("L3_12", 3, GemType.Emerald, 4, new GemCollection(3, 6, 3, 0, 0, 0)),
        ["L3_13"] = new("L3_13", 3, GemType.Ruby, 3, new GemCollection(3, 5, 3, 0, 3, 0)),
        ["L3_14"] = new("L3_14", 3, GemType.Ruby, 4, new GemCollection(7, 0, 0, 0, 0, 0)),
        ["L3_15"] = new("L3_15", 3, GemType.Ruby, 4, new GemCollection(0, 0, 0, 7, 3, 0)),
        ["L3_16"] = new("L3_16", 3, GemType.Ruby, 4, new GemCollection(0, 3, 6, 3, 0, 0)),
        ["L3_17"] = new("L3_17", 3, GemType.Onyx, 3, new GemCollection(3, 3, 5, 3, 0, 0)),
        ["L3_18"] = new("L3_18", 3, GemType.Onyx, 4, new GemCollection(0, 0, 7, 0, 0, 0)),
        ["L3_19"] = new("L3_19", 3, GemType.Onyx, 4, new GemCollection(0, 7, 0, 0, 3, 0)),
        ["L3_20"] = new("L3_20", 3, GemType.Onyx, 4, new GemCollection(0, 0, 3, 6, 3, 0))
    };

    public static Card? GetById(string id) => Cards.GetValueOrDefault(id);

    public static List<string> GetCardIdsByLevel(int level) =>
        Cards.Values
            .Where(card => card.Level == level)
            .OrderBy(card => card.Id, StringComparer.OrdinalIgnoreCase)
            .Select(card => card.Id)
            .ToList();
}
