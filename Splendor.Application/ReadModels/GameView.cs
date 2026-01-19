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
}

public class PlayerView
{
    public string Id { get; set; }
    public string OwnerId { get; set; }
    public string Name { get; set; }
    public GemCollection Gems { get; set; } = GemCollection.Empty;
    public Guid? GameViewId { get; set; }
}
