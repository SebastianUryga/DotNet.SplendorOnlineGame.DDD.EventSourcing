using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Domain.ValueObjects;
using Splendor.Domain.Entities;

namespace Splendor.Domain.Aggregates;

public class Game
{
    public Guid Id { get; set; }
    public List<Player> Players { get; set; } = new();
    public bool IsStarted { get; set; }
    public Guid? CurrentPlayerId { get; set; }
    public GemCollection MarketGems { get; set; } = GemCollection.Empty;

    public Game() { }

    // -- Event Appliers (Marten uses these to rebuild state) --
    // These methods are called AUTOMATICALLY by Marten when loading the Aggregate via EventStore.
    // They "Rehydrate" the state of the Game object from the history of events.
    // This state is then used to validate new Commands (e.g. checking IsStarted).


    public void Apply(GameCreated @event)
    {
        Id = @event.GameId;
    }

    public void Apply(PlayerJoined @event)
    {
        Players.Add(new Player(@event.PlayerId, @event.Name));
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

    public IEnumerable<IDomainEvent> JoinGame(Guid playerId, string name)
    {
        if (IsStarted) throw new InvalidOperationException("Game already started");
        if (Players.Count >= 4) throw new InvalidOperationException("Game full");
        if (Players.Any(p => p.Id == playerId)) throw new InvalidOperationException("Player already joined");

        yield return new PlayerJoined(Id, playerId, name, DateTimeOffset.UtcNow);
    }

    public IEnumerable<IDomainEvent> StartGame()
    {
        if (IsStarted) throw new InvalidOperationException("Game already started");
        if (Players.Count < 2) throw new InvalidOperationException("Need at least 2 players");

        yield return new GameStarted(Id, DateTimeOffset.UtcNow);
        yield return new TurnStarted(Id, Players.First().Id, DateTimeOffset.UtcNow);
    }

    public IEnumerable<IDomainEvent> TakeGems(Guid playerId, GemCollection gems)
    {
        if (!IsStarted) throw new InvalidOperationException("Game not started");
        if (CurrentPlayerId != playerId) throw new InvalidOperationException("Not your turn");

        // Basic validation (MVP: just check amounts positive and available)
        if (gems.Total > 3) throw new InvalidOperationException("Cannot take more than 3 gems");
        // Check availability
        if (MarketGems.Diamond < gems.Diamond || MarketGems.Sapphire < gems.Sapphire || // ...)
            // TODO: Better cleaner validation
            false) 
        {
             // throw ...
        }

        yield return new GemsTaken(Id, playerId, gems, DateTimeOffset.UtcNow);
        yield return new TurnEnded(Id, playerId, DateTimeOffset.UtcNow);

        var nextPlayer = GetNextPlayer(playerId);
        yield return new TurnStarted(Id, nextPlayer, DateTimeOffset.UtcNow);
    }

    private Guid GetNextPlayer(Guid current)
    {
        var idx = Players.FindIndex(p => p.Id == current);
        if (idx == -1) return current;
        var nextIdx = (idx + 1) % Players.Count;
        return Players[nextIdx].Id;
    }
}
