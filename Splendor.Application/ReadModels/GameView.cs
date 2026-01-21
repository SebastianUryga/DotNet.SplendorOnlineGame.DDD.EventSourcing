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

    // Card markets (visible cards)
    public List<string> Market1 { get; set; } = new();
    public List<string> Market2 { get; set; } = new();
    public List<string> Market3 { get; set; } = new();

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
    public Guid? GameViewId { get; set; }
}
