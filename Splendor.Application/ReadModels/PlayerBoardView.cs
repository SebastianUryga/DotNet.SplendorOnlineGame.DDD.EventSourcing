using Splendor.Domain.ValueObjects;

namespace Splendor.Application.ReadModels;

public class PlayerBoardView
{
    public string Id { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GemCollection Gems { get; set; } = GemCollection.Empty;
    public List<string> OwnedCardIds { get; set; } = new();
    public List<string> ReservedCardIds { get; set; } = new();
    public List<string> OwnedNobleIds { get; set; } = new();
    public int PrestigePoints { get; set; }
}
