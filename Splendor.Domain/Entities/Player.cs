using Splendor.Domain.ValueObjects;

namespace Splendor.Domain.Entities;

public class Player
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public GemCollection Gems { get; set; } = GemCollection.Empty;

    public Player(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
    
    // Construct from state if needed
    public Player() { } 
}
