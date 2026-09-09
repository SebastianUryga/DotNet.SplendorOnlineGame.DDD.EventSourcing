using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public class PlayerState
{
    public PlayerState(string ownerId, string name)
    {
        OwnerId = ownerId;
        Name = name;
    }

    public string OwnerId { get; }
    public string Name { get; }
    public GemCollection Gems { get; private set; } = GemCollection.Empty;
    public List<string> OwnedCardIds { get; } = new();
    public List<string> ReservedCardIds { get; } = new();

    public PlayerState Clone()
    {
        var clone = new PlayerState(OwnerId, Name)
        {
            Gems = Gems
        };

        clone.OwnedCardIds.AddRange(OwnedCardIds);
        clone.ReservedCardIds.AddRange(ReservedCardIds);
        return clone;
    }

    public void Apply(GemsTaken e)
    {
        Gems += e.Gems;
    }

    public void Apply(GemLimitResolved e)
    {
        Gems -= e.ReturnedGems;
    }

    public void Apply(CardReserved e)
    {
        ReservedCardIds.Add(e.CardId);
    }

    public void Apply(CardPurchased e)
    {
        OwnedCardIds.Add(e.CardId);
        ReservedCardIds.Remove(e.CardId);
        Gems -= e.PaidGems;
    }
}
