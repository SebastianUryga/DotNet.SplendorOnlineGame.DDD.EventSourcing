using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Splendor.Domain.Entities;

namespace Splendor.Domain.Aggregates;

public class Game
{
    public Guid Id { get; set; }
    public string CreatorId { get; set; } // Track who created the game
    public List<Player> Players { get; set; } = new();
    public bool IsStarted { get; set; }
    public string? CurrentPlayerId { get; set; }
    public GemCollection MarketGems { get; set; } = GemCollection.Empty;

    public Game() { }

    // -- Event Appliers (Marten uses these to rebuild state) --

    public void Apply(GameCreated @event)
    {
        Id = @event.GameId;
        CreatorId = @event.CreatorId;
    }

    public void Apply(PlayerJoined @event)
    {
        Players.Add(new Player(@event.PlayerId, @event.OwnerId, @event.Name));
    }

    public void Apply(GameStarted @event)
    {
        IsStarted = true;
        // MVP: Simple setup for 2 players. 
        // Rules: 4 of each gem (except Gold=5).
        MarketGems = new GemCollection(4, 4, 4, 4, 4, 5); 
        
        if (Players.Any())
        {
            CurrentPlayerId = Players.First().Id;
        }
    }

    public void Apply(TurnStarted @event)
    {
        CurrentPlayerId = @event.PlayerId;
    }

    public void Apply(GemsTaken @event)
    {
        MarketGems -= @event.Gems;
        var player = Players.FirstOrDefault(p => p.Id == @event.PlayerId);
        if (player != null)
        {
            player.Gems += @event.Gems;
        }
    }
    
    public void Apply(TurnEnded @event)
    {
        // Maybe nothing to do state-wise if TurnStarted handles current player
    }

    // -- Command Methods (Behavior) --

    public IEnumerable<IDomainEvent> JoinGame(string ownerId, string name)
    {
        if (IsStarted) throw new InvalidOperationException("Game already started");
        if (Players.Count >= 4) throw new InvalidOperationException("Game full");
        
        // Prevent joining with duplicate name for clarity
        if (Players.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) 
            throw new InvalidOperationException($"Player with name '{name}' already exists in this game");

        // Generate new internal Player ID (String from Guid)
        var playerId = Guid.NewGuid().ToString();
        yield return new PlayerJoined(Id, playerId, ownerId, name, DateTimeOffset.UtcNow);
    }

    public IEnumerable<IDomainEvent> StartGame(string initiatorOwnerId)
    {
        if (IsStarted) throw new InvalidOperationException("Game already started");
        if (Players.Count < 2) throw new InvalidOperationException("Need at least 2 players");
        
        // Ensure initiator is the Creator OR a participant
        bool isCreator = initiatorOwnerId == CreatorId;
        bool isParticipant = Players.Any(p => p.OwnerId == initiatorOwnerId);

        if (!isCreator && !isParticipant)
            throw new InvalidOperationException("Only the creator or a participant can start the game");

        yield return new GameStarted(Id, DateTimeOffset.UtcNow);
        yield return new TurnStarted(Id, Players.First().Id, DateTimeOffset.UtcNow);
    }

    public IEnumerable<IDomainEvent> TakeGems(string initiatorOwnerId, string playerId, GemCollection gems)
    {
        if (!IsStarted) throw new InvalidOperationException("Game not started");
        
        // 1. Find Player
        var player = Players.SingleOrDefault(p => p.Id == playerId);
        if (player == null) throw new InvalidOperationException("Player not found in this game");

        // 2. Validate Ownership
        if (player.OwnerId != initiatorOwnerId) throw new InvalidOperationException("You do not control this player");

        // 3. Validate Turn
        if (CurrentPlayerId != player.Id) throw new InvalidOperationException("Not your turn");

        // Basic validation (MVP: just check amounts positive and available)
        if (gems.Total > 3) throw new InvalidOperationException("Cannot take more than 3 gems");
        // Check availability
        if (MarketGems.Diamond < gems.Diamond || MarketGems.Sapphire < gems.Sapphire || // ...)
            // TODO: Better cleaner validation
            false) 
        {
             // throw ...
        }

        yield return new GemsTaken(Id, player.Id, gems, DateTimeOffset.UtcNow);
        yield return new TurnEnded(Id, player.Id, DateTimeOffset.UtcNow);

        var nextPlayer = GetNextPlayer(player.Id);
        yield return new TurnStarted(Id, nextPlayer, DateTimeOffset.UtcNow);
    }

    private string GetNextPlayer(string current)
    {
        var idx = Players.FindIndex(p => p.Id == current);
        if (idx == -1) return current;
        var nextIdx = (idx + 1) % Players.Count;
        return Players[nextIdx].Id;
    }
}
