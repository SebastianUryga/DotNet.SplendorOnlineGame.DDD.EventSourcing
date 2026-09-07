using Microsoft.EntityFrameworkCore;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.ReadModels;
using Splendor.Domain.Events;
using Splendor.Infrastructure.Persistence;

namespace Splendor.Infrastructure.Persistence;

public class GameReadModelProjector
{
    private readonly ReadModelsContext _context;

    public GameReadModelProjector(ReadModelsContext context)
    {
        _context = context;
    }

    public async Task ProjectAsync(object eventData, CancellationToken ct)
    {
        switch (eventData)
        {
            case GameCreated e:
                _context.GameViews.Add(new GameView
                {
                    Id = e.GameId,
                    Version = 1,
                    Status = "Created",
                    CurrentPlayerId = null,
                    Market1 = new List<string>(),
                    Market2 = new List<string>(),
                    Market3 = new List<string>()
                });
                break;

            case PlayerJoined e:
                var gameJoin = await _context.GameViews.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == e.GameId, ct);
                if (gameJoin != null)
                {
                    gameJoin.Version++;
                }

                _context.PlayerViews.Add(new PlayerView
                {
                    Id = e.PlayerId,
                    OwnerId = e.OwnerId,
                    Name = e.Name,
                    GameViewId = e.GameId
                });
                break;

            case PlayerInvited e:
                var gameInvite = await _context.GameViews.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == e.GameId, ct);
                if (gameInvite != null)
                {
                    gameInvite.Version++;
                }
                break;

            case GameStarted e:
                var gameStarted = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameStarted != null)
                {
                    gameStarted.Version++;
                    gameStarted.Status = "Started";
                    gameStarted.MarketGems = new Splendor.Domain.ValueObjects.GemCollection(4, 4, 4, 4, 4, 5);
                    gameStarted.Market1 = e.Market1?.ToList() ?? new();
                    gameStarted.Market2 = e.Market2?.ToList() ?? new();
                    gameStarted.Market3 = e.Market3?.ToList() ?? new();
                    gameStarted.Deck1Count = e.Deck1?.Count ?? 0;
                    gameStarted.Deck2Count = e.Deck2?.Count ?? 0;
                    gameStarted.Deck3Count = e.Deck3?.Count ?? 0;
                    gameStarted.Nobles = e.Nobles?.ToList() ?? new();
                }
                break;

            case TurnStarted e:
                var gameTurn = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameTurn != null)
                {
                    gameTurn.Version++;
                    gameTurn.CurrentPlayerId = e.PlayerId;
                }
                break;

            case GemsTaken e:
                var player = await _context.PlayerViews.FindAsync(new object[] { e.PlayerId }, ct);
                if (player != null)
                {
                    player.Gems = (player.Gems ?? Splendor.Domain.ValueObjects.GemCollection.Empty) + e.Gems;
                }

                var gameGems = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameGems != null)
                {
                    gameGems.Version++;
                    gameGems.MarketGems = (gameGems.MarketGems ?? Splendor.Domain.ValueObjects.GemCollection.Empty) - e.Gems;
                }
                break;

            case GemsOverflowDetected e:
                var gameOverflow = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameOverflow != null)
                {
                    gameOverflow.Version++;
                    gameOverflow.IsGemReturnPending = true;
                }
                break;

            case CardPurchased e:
                var buyingPlayer = await _context.PlayerViews.FindAsync(new object[] { e.PlayerId }, ct);
                var purchasedCard = Splendor.Domain.CardDefinitions.GetById(e.CardId);
                
                if (buyingPlayer != null)
                {
                    buyingPlayer.OwnedCardIds ??= new();
                    buyingPlayer.OwnedCardIds.Add(e.CardId);
                    buyingPlayer.Gems = (buyingPlayer.Gems ?? Splendor.Domain.ValueObjects.GemCollection.Empty) - e.PaidGems;
                    
                    if (purchasedCard != null)
                    {
                        buyingPlayer.PrestigePoints += purchasedCard.PrestigePoints;
                    }
                    // If the card was reserved by this player, remove it from their reserved list
                    if (buyingPlayer.ReservedCardIds != null && buyingPlayer.ReservedCardIds.Contains(e.CardId))
                    {
                        buyingPlayer.ReservedCardIds.Remove(e.CardId);
                    }
                }

                var gameCard = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameCard != null)
                {
                    gameCard.Version++;
                    gameCard.MarketGems = (gameCard.MarketGems ?? Splendor.Domain.ValueObjects.GemCollection.Empty) + e.PaidGems;
                    if (purchasedCard != null)
                    {
                        GetMarketForLevel(gameCard, purchasedCard.Level).Remove(e.CardId);
                    }
                }
                break;

            case CardReserved e:
                var reservingPlayer = await _context.PlayerViews.FindAsync(new object[] { e.PlayerId }, ct);
                if (reservingPlayer != null)
                {
                    reservingPlayer.ReservedCardIds ??= new();
                    reservingPlayer.ReservedCardIds.Add(e.CardId);
                }

                var gameReserved = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameReserved != null)
                {
                    gameReserved.Version++;
                    var reservedCard = Splendor.Domain.CardDefinitions.GetById(e.CardId);
                    if (reservedCard != null)
                    {
                        GetMarketForLevel(gameReserved, reservedCard.Level).Remove(e.CardId);
                    }
                }
                break;

            case NobleAcquired e:
                var noblePlayer = await _context.PlayerViews.FindAsync(new object[] { e.PlayerId }, ct);
                if (noblePlayer != null)
                {
                    noblePlayer.OwnedNobleIds ??= new();
                    noblePlayer.OwnedNobleIds.Add(e.NobleId);
                    noblePlayer.PrestigePoints += Splendor.Domain.NobleDefinitions.GetById(e.NobleId)?.PrestigePoints ?? 0;
                }

                var nobleGame = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (nobleGame != null)
                {
                    nobleGame.Version++;
                    nobleGame.Nobles ??= new();
                    nobleGame.Nobles.Remove(e.NobleId);
                    nobleGame.PlayerIdAwaitingNobleSelection = null;
                    nobleGame.EligibleNobleIds ??= new();
                    nobleGame.EligibleNobleIds.Clear();
                }
                break;

            case NobleSelectionRequired e:
                var selectionGame = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (selectionGame != null)
                {
                    selectionGame.Version++;
                    selectionGame.PlayerIdAwaitingNobleSelection = e.PlayerId;
                    selectionGame.EligibleNobleIds = e.EligibleNobleIds.ToList();
                }
                break;

            case GemLimitResolved e:
                var returnedPlayer = await _context.PlayerViews.FindAsync(new object[] { e.PlayerId }, ct);
                if (returnedPlayer != null)
                {
                    returnedPlayer.Gems = (returnedPlayer.Gems ?? Splendor.Domain.ValueObjects.GemCollection.Empty) - e.ReturnedGems;
                }

                var gameResolved = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameResolved != null)
                {
                    gameResolved.Version++;
                    gameResolved.MarketGems = (gameResolved.MarketGems ?? Splendor.Domain.ValueObjects.GemCollection.Empty) + e.ReturnedGems;
                    gameResolved.IsGemReturnPending = false;
                }
                break;

            case CardRevealed e:
                var gameReveal = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameReveal != null)
                {
                    gameReveal.Version++;
                    GetMarketForLevel(gameReveal, e.Level).Add(e.CardId);
                    DecrementDeckCount(gameReveal, e.Level);
                }
                break;

            case TurnEnded:
                // TurnEnded doesn't update read model (no state change)
                break;

            case GameFinished e:
                var gameFinished = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameFinished != null)
                {
                    gameFinished.Version++;
                    gameFinished.Status = "Finished";
                    gameFinished.WinnerId = e.WinnerId;
                    gameFinished.WinnerName = e.WinnerName;
                    gameFinished.CurrentPlayerId = null;
                }
                break;

            case GameDeleted e:
                var gameDeleted = await _context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameDeleted != null)
                {
                    gameDeleted.Version++;
                    gameDeleted.Status = "Deleted";
                }
                break;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static List<string> GetMarketForLevel(GameView game, int level)
    {
        return level switch
        {
            1 => game.Market1 ??= new(),
            2 => game.Market2 ??= new(),
            3 => game.Market3 ??= new(),
            _ => throw new ArgumentException($"Invalid market level: {level}")
        };
    }

    private static void DecrementDeckCount(GameView game, int level)
    {
        switch (level)
        {
            case 1: game.Deck1Count--; break;
            case 2: game.Deck2Count--; break;
            case 3: game.Deck3Count--; break;
            default: throw new ArgumentException($"Invalid deck level: {level}");
        }
    }
}
