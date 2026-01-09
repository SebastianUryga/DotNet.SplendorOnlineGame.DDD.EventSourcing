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
                        Status = "Created",
                        CurrentPlayerId = null
                    });
                    break;

                case PlayerJoined e:
                    _context.PlayerViews.Add(new PlayerView 
                    { 
                        Id = e.PlayerId, 
                        Name = e.Name,
                        GameViewId = e.GameId
                    });
                    break;

                case GameStarted e:
                    var gameStarted = await _context.GameViews.FindAsync(new object[] { e.GameId }, cancellationToken);
                    if (gameStarted != null)
                    {
                        gameStarted.Status = "Started";
                        gameStarted.MarketGems = new Splendor.Domain.ValueObjects.GemCollection(4, 4, 4, 4, 4, 5);
                    }
                    break;

                case TurnStarted e:
                     var gameTurn = await _context.GameViews.FindAsync(new object[] { e.GameId }, cancellationToken);
                     if (gameTurn != null) gameTurn.CurrentPlayerId = e.PlayerId;
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
                        gameGems.MarketGems -= e.Gems;
                    }
                    break;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
