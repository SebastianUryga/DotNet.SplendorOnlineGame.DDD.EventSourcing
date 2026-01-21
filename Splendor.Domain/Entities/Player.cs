using Splendor.Domain.ValueObjects;

namespace Splendor.Domain.Entities;

public class Player
{
    public string Id { get; set; }
    public string OwnerId { get; set; }
    public string Name { get; set; }
    public GemCollection Gems { get; set; } = GemCollection.Empty;
    public List<string> OwnedCardIds { get; set; } = new();

    public Player(string id, string ownerId, string name)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
    }
    
    // Construct from state if needed
    public Player() { } 
}
