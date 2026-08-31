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
    public string Status { get; private set; } = "Created";
    public string? CurrentPlayerId { get; set; }
    public GemCollection MarketGems { get; set; } = GemCollection.Empty;
    // When a player exceeded the gem limit and must return some gems
    public string? PlayerIdAwaitingGemReturn { get; set; }
    public int ExcessGemsToReturn { get; set; }

    // Card decks (remaining cards to draw)
    public List<string> Deck1 { get; set; } = new();
    public List<string> Deck2 { get; set; } = new();
    public List<string> Deck3 { get; set; } = new();

    // Visible cards on table (4 per level)
    public List<string> Market1 { get; set; } = new();
    public List<string> Market2 { get; set; } = new();
    public List<string> Market3 { get; set; } = new();

    // Nobles selected for this game (ids)
    public List<string> Nobles { get; set; } = new();
    public string? PlayerIdAwaitingNobleSelection { get; set; }

    public Game() { }

    private void EnsureStarted()
    {
        if (Status != "Started") throw new InvalidOperationException("Game not started");
    }

    private void EnsureNotFinished()
    {
        if (Status == "Finished") throw new InvalidOperationException("Game is already finished.");
    }

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
        Status = "Started";
        MarketGems = new GemCollection(4, 4, 4, 4, 4, 5);

        Deck1 = @event.Deck1.ToList();
        Deck2 = @event.Deck2.ToList();
        Deck3 = @event.Deck3.ToList();
        Market1 = @event.Market1.ToList();
        Market2 = @event.Market2.ToList();
        Market3 = @event.Market3.ToList();
        Nobles = @event.Nobles?.ToList() ?? new();

        if (Players.Any())
        {
            CurrentPlayerId = Players.First().Id;
        }
    }

    public void Apply(CardReserved @event)
    {
        var player = Players.FirstOrDefault(p => p.Id == @event.PlayerId);
        if (player != null)
        {
            player.ReservedCardIds ??= new();
            player.ReservedCardIds.Add(@event.CardId);
        }

        // Remove card from market if present
        var card = CardDefinitions.GetById(@event.CardId);
        if (card != null)
        {
            GetMarketForLevel(card.Level).Remove(@event.CardId);
        }
    }

    public void Apply(TurnStarted @event)
    {
        CurrentPlayerId = @event.PlayerId;
    }

    public void Apply(GameFinished @event)
    {
        Status = "Finished";
        CurrentPlayerId = null;
    }

    public void Apply(GameDeleted @event)
    {
        Status = "Deleted";
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

    public void Apply(GemsOverflowDetected @event)
    {
        // mark that this player must return excess gems
        PlayerIdAwaitingGemReturn = @event.PlayerId;
        ExcessGemsToReturn = @event.ExcessCount;
        // we also keep current gems reflected by GemsTaken application
    }

    public void Apply(GemLimitResolved @event)
    {
        // apply returned gems to player and return them to market
        var player = Players.FirstOrDefault(p => p.Id == @event.PlayerId);
        if (player != null)
        {
            player.Gems -= @event.ReturnedGems;
        }

        MarketGems += @event.ReturnedGems;
        // clear pending return
        PlayerIdAwaitingGemReturn = null;
        ExcessGemsToReturn = 0;
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
            // If the card was previously reserved by the player, ensure it's removed from their reserved list
            var ownerPlayer = Players.FirstOrDefault(p => p.Id == @event.PlayerId);
            if (ownerPlayer != null && ownerPlayer.ReservedCardIds != null && ownerPlayer.ReservedCardIds.Contains(@event.CardId))
            {
                ownerPlayer.ReservedCardIds.Remove(@event.CardId);
            }
        }
    }

    public void Apply(CardRevealed @event)
    {
        GetMarketForLevel(@event.Level).Add(@event.CardId);
        GetDeckForLevel(@event.Level).Remove(@event.CardId);
    }

    public void Apply(NobleSelectionRequired @event)
    {
        PlayerIdAwaitingNobleSelection = @event.PlayerId;
    }

    public void Apply(NobleAcquired @event)
    {
        var player = Players.FirstOrDefault(p => p.Id == @event.PlayerId);
        if (player != null)
        {
            player.OwnedNobleIds.Add(@event.NobleId);
        }

        Nobles.Remove(@event.NobleId);
        PlayerIdAwaitingNobleSelection = null;
    }

    // -- Command Methods (Behavior) --

    public IEnumerable<IDomainEvent> DeleteGame()
    {
        if (Status == "Deleted") throw new InvalidOperationException("Game is already deleted.");
        yield return new GameDeleted(Id, DateTimeOffset.UtcNow);
    }

    public IEnumerable<IDomainEvent> JoinGame(string ownerId, string name)
    {
        EnsureActive();
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
        if (Status != "Created") throw new InvalidOperationException("Game already started");
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

        // Pick 3 random nobles for this game
        var nobleIds = new List<string>();
        var allNobles = NobleDefinitions.AllNobles.Select(n => n.Id).ToList();
        // shuffle nobles
        allNobles = allNobles.OrderBy(_ => random.Next()).ToList();
        nobleIds = allNobles.Take(Math.Min(3, allNobles.Count)).ToList();

        yield return new GameStarted(Id, deck1, deck2, deck3, market1, market2, market3, nobleIds, DateTimeOffset.UtcNow);
        yield return new TurnStarted(Id, Players.First().Id, DateTimeOffset.UtcNow);
    }

    public IEnumerable<IDomainEvent> TakeGems(string initiatorOwnerId, string playerId, GemCollection gems)
    {
        EnsureStarted();
        EnsureNotFinished();

        // 1. Find Player
        var player = Players.SingleOrDefault(p => p.Id == playerId);
        if (player == null) throw new InvalidOperationException("Player not found in this game");

        // 2. Validate Ownership
        if (player.OwnerId != initiatorOwnerId) throw new InvalidOperationException("You do not control this player");

        // 3. Validate Turn
        if (CurrentPlayerId != player.Id) throw new InvalidOperationException("Not your turn");

        if (!string.IsNullOrEmpty(PlayerIdAwaitingGemReturn))
            throw new InvalidOperationException("A gem overflow resolution is pending. No other actions are allowed until the gem limit is resolved.");

        if (!string.IsNullOrEmpty(PlayerIdAwaitingNobleSelection))
            throw new InvalidOperationException("A noble selection is pending. No other actions are allowed until the noble is selected.");

        var colorCounts = new[] { gems.Diamond, gems.Sapphire, gems.Emerald, gems.Ruby, gems.Onyx };
        var nonZeroColors = colorCounts.Where(c => c > 0).ToList();

        bool isOptionA = gems.Gold == 0 && nonZeroColors.All(c => c == 1) && nonZeroColors.Count >= 1 && nonZeroColors.Count <= 3;
        bool isOptionB = gems.Gold == 0 && nonZeroColors.Count == 1 && nonZeroColors[0] == 2;
        bool isOptionC = gems.Gold == 1 && nonZeroColors.Count == 0;

        if (!isOptionA && !isOptionB && !isOptionC)
            throw new InvalidOperationException("Invalid gem selection.");

        if (isOptionB)
        {
            if (gems.Diamond == 2 && MarketGems.Diamond < 4) throw new InvalidOperationException("Not enough diamonds on market.");
            if (gems.Sapphire == 2 && MarketGems.Sapphire < 4) throw new InvalidOperationException("Not enough sapphires on market.");
            if (gems.Emerald == 2 && MarketGems.Emerald < 4) throw new InvalidOperationException("Not enough emeralds on market.");
            if (gems.Ruby == 2 && MarketGems.Ruby < 4) throw new InvalidOperationException("Not enough rubies on market.");
            if (gems.Onyx == 2 && MarketGems.Onyx < 4) throw new InvalidOperationException("Not enough onyxes on market.");
        }

        if (MarketGems.Diamond < gems.Diamond) throw new InvalidOperationException("Not enough diamonds on market.");
        if (MarketGems.Sapphire < gems.Sapphire) throw new InvalidOperationException("Not enough sapphires on market.");
        if (MarketGems.Emerald < gems.Emerald) throw new InvalidOperationException("Not enough emeralds on market.");
        if (MarketGems.Ruby < gems.Ruby) throw new InvalidOperationException("Not enough rubies on market.");
        if (MarketGems.Onyx < gems.Onyx) throw new InvalidOperationException("Not enough onyxes on market.");
        if (MarketGems.Gold < gems.Gold) throw new InvalidOperationException("Not enough gold on market.");

        // Emit GemsTaken first
        yield return new GemsTaken(Id, player.Id, gems, DateTimeOffset.UtcNow);

        // Calculate player's new total after taking
        var newTotal = player.Gems + gems;
        if (newTotal.Total > 10)
        {
            // Player must return excess gems - emit overflow detected and set pending state
            var excess = newTotal.Total - 10;
            yield return new GemsOverflowDetected(Id, player.Id, newTotal, excess, DateTimeOffset.UtcNow);
            // TurnEnded will be emitted only after player resolves the gem limit via ResolveGemLimit
            yield break;
        }

        foreach (var @event in CompleteTurn(player))
        {
            yield return @event;
        }
    }

    private string GetNextPlayer(string current)
    {
        var idx = Players.FindIndex(p => p.Id == current);
        if (idx == -1) return current;
        var nextIdx = (idx + 1) % Players.Count;
        return Players[nextIdx].Id;
    }

    public IEnumerable<IDomainEvent> ResolveGemLimit(string initiatorOwnerId, string playerId, GemCollection returnedGems)
    {
        EnsureStarted();
        EnsureNotFinished();

        var player = Players.SingleOrDefault(p => p.Id == playerId);
        if (player == null) throw new InvalidOperationException("Player not found");
        if (player.OwnerId != initiatorOwnerId) throw new InvalidOperationException("You do not control this player");

        if (PlayerIdAwaitingGemReturn != playerId)
            throw new InvalidOperationException("No gem return is required for this player");

        // Validate returned gems are actually owned by the player
        if (returnedGems.Diamond > player.Gems.Diamond) throw new InvalidOperationException("Cannot return more diamonds than owned");
        if (returnedGems.Sapphire > player.Gems.Sapphire) throw new InvalidOperationException("Cannot return more sapphires than owned");
        if (returnedGems.Emerald > player.Gems.Emerald) throw new InvalidOperationException("Cannot return more emeralds than owned");
        if (returnedGems.Ruby > player.Gems.Ruby) throw new InvalidOperationException("Cannot return more rubies than owned");
        if (returnedGems.Onyx > player.Gems.Onyx) throw new InvalidOperationException("Cannot return more onyxes than owned");
        if (returnedGems.Gold > player.Gems.Gold) throw new InvalidOperationException("Cannot return more gold than owned");

        var newTotal = player.Gems - returnedGems;
        if (newTotal.Total > 10) throw new InvalidOperationException("Returned gems do not reduce total to allowed limit");

        yield return new GemLimitResolved(Id, player.Id, returnedGems, DateTimeOffset.UtcNow);

        foreach (var @event in CompleteTurn(player))
        {
            yield return @event;
        }
    }

    public IEnumerable<IDomainEvent> BuyCard(string initiatorOwnerId, string playerId, string cardId)
    {
        EnsureStarted();
        EnsureNotFinished();

        var player = Players.SingleOrDefault(p => p.Id == playerId);
        if (player == null) throw new InvalidOperationException("Player not found");
        if (player.OwnerId != initiatorOwnerId) throw new InvalidOperationException("You do not control this player");
        if (CurrentPlayerId != player.Id) throw new InvalidOperationException("Not your turn");

        var card = CardDefinitions.GetById(cardId);
        if (card == null) throw new InvalidOperationException("Card not found");

        var market = GetMarketForLevel(card.Level);
        var isReservedByPlayer = player.ReservedCardIds != null && player.ReservedCardIds.Contains(cardId);
        if (!market.Contains(cardId) && !isReservedByPlayer) throw new InvalidOperationException("Card not available in market or reserved by player");

        if (!string.IsNullOrEmpty(PlayerIdAwaitingGemReturn))
            throw new InvalidOperationException("A gem overflow resolution is pending. No other actions are allowed until the gem limit is resolved.");

        if (!string.IsNullOrEmpty(PlayerIdAwaitingNobleSelection))
            throw new InvalidOperationException("A noble selection is pending. No other actions are allowed until the noble is selected.");

        // Calculate effective cost (subtract bonuses from owned cards)
        var bonuses = GetPlayerBonuses(player);
        var effectiveCost = CalculateEffectiveCost(card.Cost, bonuses);

        // Check if player can afford it
        if (!CanAfford(player.Gems, effectiveCost))
            throw new InvalidOperationException("Cannot afford this card");

        // Calculate actual payment (may use gold as wildcards)
        var payment = CalculatePayment(player.Gems, effectiveCost);

        // Emit purchase
        yield return new CardPurchased(Id, player.Id, cardId, payment, DateTimeOffset.UtcNow);

        // If the card was taken from the market (not a previously reserved card), reveal replacement
        if (market.Contains(cardId))
        {
            var deck = GetDeckForLevel(card.Level);
            if (deck.Any())
            {
                yield return new CardRevealed(Id, card.Level, deck.First(), DateTimeOffset.UtcNow);
            }
        }

        player.OwnedCardIds.Add(cardId);
        foreach (var @event in CompleteTurn(player))
        {
            yield return @event;
        }
    }

    public IEnumerable<IDomainEvent> ReserveCard(string ownerId, string playerId, string cardId)
    {
        EnsureStarted();
        EnsureNotFinished();

        var player = Players.SingleOrDefault(p => p.Id == playerId);
        if (player == null) throw new InvalidOperationException("Player not found");
        if (player.OwnerId != ownerId) throw new InvalidOperationException("You do not control this player");
        if (CurrentPlayerId != player.Id) throw new InvalidOperationException("Not your turn");

        if (!string.IsNullOrEmpty(PlayerIdAwaitingGemReturn))
            throw new InvalidOperationException("A gem overflow resolution is pending. No other actions are allowed until the gem limit is resolved.");

        if (!string.IsNullOrEmpty(PlayerIdAwaitingNobleSelection))
            throw new InvalidOperationException("A noble selection is pending. No other actions are allowed until the noble is selected.");

        // Ensure player does not have more than 3 reserved cards
        player.ReservedCardIds ??= new();
        if (player.ReservedCardIds.Count >= 3) throw new InvalidOperationException("Player already has maximum number of reserved cards (3)");

        var card = CardDefinitions.GetById(cardId);
        if (card == null) throw new InvalidOperationException("Card not found");

        // Only allow reserving cards that are currently in market
        var market = GetMarketForLevel(card.Level);
        if (!market.Contains(cardId)) throw new InvalidOperationException("Card not available in market");

        // If gold available on market, give one gold to player
        var goldTaken = new GemCollection(0,0,0,0,0,0);
        if (MarketGems.Gold > 0)
        {
            goldTaken = new GemCollection(0,0,0,0,0,1);
            yield return new GemsTaken(Id, player.Id, goldTaken, DateTimeOffset.UtcNow);
        }

        // Reserve the card
        yield return new CardReserved(Id, player.Id, cardId, DateTimeOffset.UtcNow);

        // Reveal new card from deck if available
        var deck = GetDeckForLevel(card.Level);
        if (deck.Any())
        {
            yield return new CardRevealed(Id, card.Level, deck.First(), DateTimeOffset.UtcNow);
        }

        // If gold was given, check for overflow
        if (goldTaken.Gold > 0)
        {
            var newTotal = player.Gems + goldTaken;
            if (newTotal.Total > 10)
            {
                var excess = newTotal.Total - 10;
                yield return new GemsOverflowDetected(Id, player.Id, newTotal, excess, DateTimeOffset.UtcNow);
                yield break;
            }
        }

        foreach (var @event in CompleteTurn(player))
        {
            yield return @event;
        }
    }

    private IEnumerable<IDomainEvent> CompleteTurn(Player player)
    {
        var eligibleNobles = GetEligibleNobles(player);
        var acquiredNoble = eligibleNobles.Count == 1 ? eligibleNobles.Single() : null;

        if (eligibleNobles.Count > 1)
        {
            yield return new NobleSelectionRequired(Id, player.Id, eligibleNobles.Select(n => n.Id).ToList(), DateTimeOffset.UtcNow);
            yield break;
        }

        if (acquiredNoble != null)
        {
            yield return new NobleAcquired(Id, player.Id, acquiredNoble.Id, DateTimeOffset.UtcNow);
        }

        var totalPoints = GetPlayerPrestigePoints(player) + (acquiredNoble?.PrestigePoints ?? 0);
        if (totalPoints >= 15)
        {
            yield return new GameFinished(Id, player.Id, player.Name, totalPoints, DateTimeOffset.UtcNow);
            yield break;
        }

        yield return new TurnEnded(Id, player.Id, DateTimeOffset.UtcNow);
        yield return new TurnStarted(Id, GetNextPlayer(player.Id), DateTimeOffset.UtcNow);
    }

    private List<Noble> GetEligibleNobles(Player player)
    {
        var bonuses = GetPlayerBonuses(player);
        return Nobles
            .Select(NobleDefinitions.GetById)
            .Where(noble => noble != null && MeetsNobleRequirements(bonuses, noble.Requirements))
            .Cast<Noble>()
            .ToList();
    }

    private static bool MeetsNobleRequirements(GemCollection bonuses, GemCollection requirements) =>
        bonuses.Diamond >= requirements.Diamond &&
        bonuses.Sapphire >= requirements.Sapphire &&
        bonuses.Emerald >= requirements.Emerald &&
        bonuses.Ruby >= requirements.Ruby &&
        bonuses.Onyx >= requirements.Onyx;

    private int GetPlayerPrestigePoints(Player player)
    {
        return player.OwnedCardIds.Sum(id => CardDefinitions.GetById(id)?.PrestigePoints ?? 0) +
            player.OwnedNobleIds.Sum(id => NobleDefinitions.GetById(id)?.PrestigePoints ?? 0);
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

    private void EnsureActive()
    {
        if (Status == "Started") throw new InvalidOperationException("Game is already started.");
        if (Status == "Finished") throw new InvalidOperationException("Game is already finished.");
        if (Status == "Deleted") throw new InvalidOperationException("Game has been deleted.");
    }

}
