using Splendor.Domain.ValueObjects;
using Splendor.Domain.Events;

namespace Splendor.Application.DecisionStates;

public class PlayerState
{
    public PlayerState(string ownerId, string name)
    {
        OwnerId = ownerId;
        Name = name;
    }

    public string OwnerId { get; set; }
    public string Name { get; set; }
    public GemCollection Gems { get; set; } = GemCollection.Empty;
    public List<string> OwnedCardIds { get; set; } = new();
    public List<string> ReservedCardIds { get; set; } = new();
    public List<string> OwnedNobleIds { get; set; } = new();

    public PlayerState Clone()
    {
        var clone = new PlayerState(OwnerId, Name)
        {
            Gems = Gems
        };

        clone.OwnedCardIds.AddRange(OwnedCardIds);
        clone.ReservedCardIds.AddRange(ReservedCardIds);
        clone.OwnedNobleIds.AddRange(OwnedNobleIds);
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

    public void Apply(NobleAcquired e)
    {
        OwnedNobleIds.Add(e.NobleId);
    }
}
