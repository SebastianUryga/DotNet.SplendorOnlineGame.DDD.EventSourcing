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

    // Card decks (remaining cards to draw)
    public List<string> Deck1 { get; set; } = new();
    public List<string> Deck2 { get; set; } = new();
    public List<string> Deck3 { get; set; } = new();

    // Visible cards on table (4 per level)
    public List<string> Market1 { get; set; } = new();
    public List<string> Market2 { get; set; } = new();
    public List<string> Market3 { get; set; } = new();

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
        MarketGems = new GemCollection(4, 4, 4, 4, 4, 5);

        Deck1 = @event.Deck1.ToList();
        Deck2 = @event.Deck2.ToList();
        Deck3 = @event.Deck3.ToList();
        Market1 = @event.Market1.ToList();
        Market2 = @event.Market2.ToList();
        Market3 = @event.Market3.ToList();

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

    public void Apply(CardPurchased @event)
    {
        var player = Players.FirstOrDefault(p => p.Id == @event.PlayerId);
        if (player != null)
        {
            player.OwnedCardIds.Add(@event.CardId);
            player.Gems -= @event.PaidGems;
        }
        MarketGems += @event.PaidGems;

        // Remove card from market
        var card = CardDefinitions.GetById(@event.CardId);
        if (card != null)
        {
            GetMarketForLevel(card.Level).Remove(@event.CardId);
        }
    }

    public void Apply(CardRevealed @event)
    {
        GetMarketForLevel(@event.Level).Add(@event.CardId);
        GetDeckForLevel(@event.Level).Remove(@event.CardId);
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
        var playerId = Guid.NewGuid().ToString() + " " + name;
        yield return new PlayerJoined(Id, playerId, ownerId, name, DateTimeOffset.UtcNow);
    }

    public IEnumerable<IDomainEvent> StartGame(string initiatorOwnerId)
    {
        if (IsStarted) throw new InvalidOperationException("Game already started");
        if (Players.Count < 2) throw new InvalidOperationException("Need at least 2 players");

        bool isCreator = initiatorOwnerId == CreatorId;
        bool isParticipant = Players.Any(p => p.OwnerId == initiatorOwnerId);

        if (!isCreator && !isParticipant)
            throw new InvalidOperationException("Only the creator or a participant can start the game");

        // Shuffle and setup card decks
        var random = new Random();
        var deck1 = CardDefinitions.GetLevel(1).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();
        var deck2 = CardDefinitions.GetLevel(2).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();
        var deck3 = CardDefinitions.GetLevel(3).Select(c => c.Id).OrderBy(_ => random.Next()).ToList();

        // Draw 4 cards for each market
        var market1 = deck1.Take(4).ToList();
        deck1 = deck1.Skip(4).ToList();
        var market2 = deck2.Take(4).ToList();
        deck2 = deck2.Skip(4).ToList();
        var market3 = deck3.Take(4).ToList();
        deck3 = deck3.Skip(4).ToList();

        yield return new GameStarted(Id, deck1, deck2, deck3, market1, market2, market3, DateTimeOffset.UtcNow);
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

    public IEnumerable<IDomainEvent> BuyCard(string initiatorOwnerId, string playerId, string cardId)
    {
        if (!IsStarted) throw new InvalidOperationException("Game not started");

        var player = Players.SingleOrDefault(p => p.Id == playerId);
        if (player == null) throw new InvalidOperationException("Player not found");
        if (player.OwnerId != initiatorOwnerId) throw new InvalidOperationException("You do not control this player");
        if (CurrentPlayerId != player.Id) throw new InvalidOperationException("Not your turn");

        var card = CardDefinitions.GetById(cardId);
        if (card == null) throw new InvalidOperationException("Card not found");

        var market = GetMarketForLevel(card.Level);
        if (!market.Contains(cardId)) throw new InvalidOperationException("Card not available in market");

        // Calculate effective cost (subtract bonuses from owned cards)
        var bonuses = GetPlayerBonuses(player);
        var effectiveCost = CalculateEffectiveCost(card.Cost, bonuses);

        // Check if player can afford it
        if (!CanAfford(player.Gems, effectiveCost))
            throw new InvalidOperationException("Cannot afford this card");

        // Calculate actual payment (may use gold as wildcards)
        var payment = CalculatePayment(player.Gems, effectiveCost);

        yield return new CardPurchased(Id, player.Id, cardId, payment, DateTimeOffset.UtcNow);

        // Reveal new card from deck
        var deck = GetDeckForLevel(card.Level);
        if (deck.Any())
        {
            yield return new CardRevealed(Id, card.Level, deck.First(), DateTimeOffset.UtcNow);
        }

        yield return new TurnEnded(Id, player.Id, DateTimeOffset.UtcNow);
        yield return new TurnStarted(Id, GetNextPlayer(player.Id), DateTimeOffset.UtcNow);
    }

    private GemCollection GetPlayerBonuses(Player player)
    {
        int diamond = 0, sapphire = 0, emerald = 0, ruby = 0, onyx = 0;
        foreach (var cardId in player.OwnedCardIds)
        {
            var card = CardDefinitions.GetById(cardId);
            if (card == null) continue;
            switch (card.BonusType)
            {
                case GemType.Diamond: diamond++; break;
                case GemType.Sapphire: sapphire++; break;
                case GemType.Emerald: emerald++; break;
                case GemType.Ruby: ruby++; break;
                case GemType.Onyx: onyx++; break;
            }
        }
        return new GemCollection(diamond, sapphire, emerald, ruby, onyx, 0);
    }

    private GemCollection CalculateEffectiveCost(GemCollection cost, GemCollection bonuses)
    {
        return new GemCollection(
            Math.Max(0, cost.Diamond - bonuses.Diamond),
            Math.Max(0, cost.Sapphire - bonuses.Sapphire),
            Math.Max(0, cost.Emerald - bonuses.Emerald),
            Math.Max(0, cost.Ruby - bonuses.Ruby),
            Math.Max(0, cost.Onyx - bonuses.Onyx),
            0
        );
    }

    private bool CanAfford(GemCollection playerGems, GemCollection cost)
    {
        int deficit = 0;
        deficit += Math.Max(0, cost.Diamond - playerGems.Diamond);
        deficit += Math.Max(0, cost.Sapphire - playerGems.Sapphire);
        deficit += Math.Max(0, cost.Emerald - playerGems.Emerald);
        deficit += Math.Max(0, cost.Ruby - playerGems.Ruby);
        deficit += Math.Max(0, cost.Onyx - playerGems.Onyx);
        return deficit <= playerGems.Gold;
    }

    private GemCollection CalculatePayment(GemCollection playerGems, GemCollection cost)
    {
        int goldNeeded = 0;
        int dPay = Math.Min(playerGems.Diamond, cost.Diamond);
        goldNeeded += cost.Diamond - dPay;
        int sPay = Math.Min(playerGems.Sapphire, cost.Sapphire);
        goldNeeded += cost.Sapphire - sPay;
        int ePay = Math.Min(playerGems.Emerald, cost.Emerald);
        goldNeeded += cost.Emerald - ePay;
        int rPay = Math.Min(playerGems.Ruby, cost.Ruby);
        goldNeeded += cost.Ruby - rPay;
        int oPay = Math.Min(playerGems.Onyx, cost.Onyx);
        goldNeeded += cost.Onyx - oPay;

        return new GemCollection(dPay, sPay, ePay, rPay, oPay, goldNeeded);
    }

    private List<string> GetMarketForLevel(int level) => level switch
    {
        1 => Market1,
        2 => Market2,
        3 => Market3,
        _ => throw new ArgumentException("Invalid level")
    };

    private List<string> GetDeckForLevel(int level) => level switch
    {
        1 => Deck1,
        2 => Deck2,
        3 => Deck3,
        _ => throw new ArgumentException("Invalid level")
    };
}
