using Splendor.Domain.ValueObjects;

namespace Splendor.Domain;

public static class NobleDefinitions
{
    public static readonly IReadOnlyList<Noble> AllNobles = new List<Noble>
    {
        new("N_01", 3, new GemCollection(3,3,3,0,0,0), "Merchant of Diamonds"),
        new("N_02", 3, new GemCollection(0,3,3,3,0,0), "Sapphire Patron"),
        new("N_03", 3, new GemCollection(0,0,3,3,3,0), "Emerald Benefactor"),
        new("N_04", 3, new GemCollection(3,0,0,3,3,0), "Ruby Noble"),
        new("N_05", 3, new GemCollection(3,0,3,0,3,0), "Collector of Onyx"),
        new("N_06", 3, new GemCollection(4,0,0,0,4,0), "Diamond Onyx Duke"),
        new("N_07", 3, new GemCollection(4,4,0,0,0,0), "Diamond Sapphire Duke"),
        new("N_08", 3, new GemCollection(0,4,4,0,0,0), "Sapphire Emerald Duke"),
        new("N_09", 3, new GemCollection(0,0,4,4,0,0), "Emerald Ruby Duke"),
        new("N_10", 3, new GemCollection(0,0,0,4,4,0), "Ruby Onyx Duke")
    };

    public static Noble? GetById(string id) => AllNobles.FirstOrDefault(n => n.Id == id);
}
