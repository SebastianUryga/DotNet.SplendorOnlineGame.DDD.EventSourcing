using Splendor.Domain.ValueObjects;

namespace Splendor.Application.ReadModels;

public class GameView
{
    public Guid Id { get; set; }
    public long Version { get; set; }
    public string Status { get; set; } = "Created";
    public List<PlayerView> Players { get; set; } = new();
    public GemCollection MarketGems { get; set; } = GemCollection.Empty;
    public string? CurrentPlayerId { get; set; }
    public string? WinnerId { get; set; }
    public string? WinnerName { get; set; }

    // Indicates that a player exceeded the gem limit and must return gems
    public bool IsGemReturnPending { get; set; } = false;

    // Card markets (visible cards)
    public List<string> Market1 { get; set; } = new();
    public List<string> Market2 { get; set; } = new();
    public List<string> Market3 { get; set; } = new();

    // Nobles selected for this game (ids)
    public List<string> Nobles { get; set; } = new();
    public string? PlayerIdAwaitingNobleSelection { get; set; }
    public List<string> EligibleNobleIds { get; set; } = new();

    // Remaining cards in decks (just count for display)
    public int Deck1Count { get; set; }
    public int Deck2Count { get; set; }
    public int Deck3Count { get; set; }
}

public class PlayerView
{
    public string Id { get; set; }
    public string OwnerId { get; set; }
    public string Name { get; set; }
    public GemCollection Gems { get; set; } = GemCollection.Empty;
    public List<string> OwnedCardIds { get; set; } = new();
    public List<string> ReservedCardIds { get; set; } = new();
    public int PrestigePoints { get; set; }
    public Guid? GameViewId { get; set; }
    public List<string> OwnedNobleIds { get; set; } = new();
}
