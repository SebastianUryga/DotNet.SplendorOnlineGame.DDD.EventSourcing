using Microsoft.EntityFrameworkCore;
using Splendor.Application.ReadModels;
using Splendor.Domain.Entities;
using Splendor.Domain.Events;
using Splendor.Infrastructure.Persistence;

using Splendor.Application.Common.Interfaces;

namespace Splendor.Infrastructure.Projections;

public class GameReadModelProjector : IGameReadModelProjector
{
    private readonly ReadModelsContext _context;

    public GameReadModelProjector(ReadModelsContext context)
    {
        _context = context;
    }

    public async Task ProjectAsync(IEnumerable<object> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            switch (@event)
            {
                case GameCreated e:
                    _context.GameViews.Add(new GameView 
                    { 
                        Id = e.GameId, 
                        Version = 1,
                        Status = "Created",
                        CurrentPlayerId = null
                    });
                    break;

                case PlayerJoined e:
                    // PlayerJoined usually happens before game starts, but could happen anytime if we allowed it
                    // We need to fetch the game view to add player and update version
                     var gameJoin = await _context.GameViews.Include(g => g.Players).FirstOrDefaultAsync(g => g.Id == e.GameId, cancellationToken);
                     if (gameJoin != null)
                     {
                        gameJoin.Version++;
                     }

                    _context.PlayerViews.Add(new PlayerView 
                    { 
                        Id = e.PlayerId,     
                        OwnerId = e.OwnerId, // String
                        Name = e.Name,
                        GameViewId = e.GameId
                    });
                    break;

                case GameStarted e:
                    var gameStarted = await _context.GameViews.FindAsync(new object[] { e.GameId }, cancellationToken);
                    if (gameStarted != null)
                    {
                        gameStarted.Version++;
                        gameStarted.Status = "Started";
                        gameStarted.MarketGems = new Splendor.Domain.ValueObjects.GemCollection(4, 4, 4, 4, 4, 5);
                    }
                    break;

                case TurnStarted e:
                     var gameTurn = await _context.GameViews.FindAsync(new object[] { e.GameId }, cancellationToken);
                     if (gameTurn != null) 
                     {
                        gameTurn.Version++;
                        gameTurn.CurrentPlayerId = e.PlayerId;
                     }
                     break;

                case GemsTaken e:
                    var player = await _context.PlayerViews.FindAsync(new object[] { e.PlayerId }, cancellationToken);
                    if (player != null)
                    {
                        player.Gems += e.Gems;
                    }
                    
                    var gameGems = await _context.GameViews.FindAsync(new object[] { e.GameId }, cancellationToken);
                    if (gameGems != null)
                    {
                        gameGems.Version++;
                        gameGems.MarketGems -= e.Gems;
                    }
                    break;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
