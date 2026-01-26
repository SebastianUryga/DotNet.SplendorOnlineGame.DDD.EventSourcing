using Marten;
using Marten.Events;
using Marten.Events.Daemon;
using Marten.Events.Daemon.Internals;
using Marten.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Splendor.Application.ReadModels;
using Splendor.Domain.Events;
using Splendor.Infrastructure.Persistence;

namespace Splendor.Infrastructure.Projections;

public class GameReadModelSubscription : SubscriptionBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GameReadModelSubscription(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        SubscriptionName = "GameReadModelProjection";

        IncludeType<GameCreated>();
        IncludeType<PlayerJoined>();
        IncludeType<GameStarted>();
        IncludeType<TurnStarted>();
        IncludeType<GemsTaken>();
        IncludeType<CardPurchased>();
        IncludeType<CardRevealed>();
        IncludeType<TurnEnded>();
    }

    public override async Task<IChangeListener> ProcessEventsAsync(
        EventRange page,
        ISubscriptionController controller,
        IDocumentOperations operations,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReadModelsContext>();

        foreach (var @event in page.Events)
        {
            await ApplyEventAsync(@event.Data, context, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return NullChangeListener.Instance;
    }

    private async Task ApplyEventAsync(object eventData, ReadModelsContext context, CancellationToken ct)
    {
        switch (eventData)
        {
            case GameCreated e:
                context.GameViews.Add(new GameView
                {
                    Id = e.GameId,
                    Version = 1,
                    Status = "Created",
                    CurrentPlayerId = null
                });
                break;

            case PlayerJoined e:
                var gameJoin = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.GameViews.Include(g => g.Players), g => g.Id == e.GameId, ct);
                if (gameJoin != null)
                {
                    gameJoin.Version++;
                }

                context.PlayerViews.Add(new PlayerView
                {
                    Id = e.PlayerId,
                    OwnerId = e.OwnerId,
                    Name = e.Name,
                    GameViewId = e.GameId
                });
                break;

            case GameStarted e:
                var gameStarted = await context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameStarted != null)
                {
                    gameStarted.Version++;
                    gameStarted.Status = "Started";
                    gameStarted.MarketGems = new Splendor.Domain.ValueObjects.GemCollection(4, 4, 4, 4, 4, 5);
                    gameStarted.Market1 = e.Market1.ToList();
                    gameStarted.Market2 = e.Market2.ToList();
                    gameStarted.Market3 = e.Market3.ToList();
                    gameStarted.Deck1Count = e.Deck1.Count;
                    gameStarted.Deck2Count = e.Deck2.Count;
                    gameStarted.Deck3Count = e.Deck3.Count;
                }
                break;

            case TurnStarted e:
                var gameTurn = await context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameTurn != null)
                {
                    gameTurn.Version++;
                    gameTurn.CurrentPlayerId = e.PlayerId;
                }
                break;

            case GemsTaken e:
                var player = await context.PlayerViews.FindAsync(new object[] { e.PlayerId }, ct);
                if (player != null)
                {
                    player.Gems += e.Gems;
                }

                var gameGems = await context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameGems != null)
                {
                    gameGems.Version++;
                    gameGems.MarketGems -= e.Gems;
                }
                break;

            case CardPurchased e:
                var buyingPlayer = await context.PlayerViews.FindAsync(new object[] { e.PlayerId }, ct);
                if (buyingPlayer != null)
                {
                    buyingPlayer.OwnedCardIds.Add(e.CardId);
                    buyingPlayer.Gems -= e.PaidGems;
                }

                var gameCard = await context.GameViews.FindAsync(new object[] { e.GameId }, ct);
                if (gameCard != null)
                {
                    gameCard.Version++;
                    gameCard.MarketGems += e.PaidGems;
                    var card = Splendor.Domain.CardDefinitions.GetById(e.CardId);
                    if (card != null)
                    {
                        GetMarketForLevel(gameCard, card.Level).Remove(e.CardId);
                    }
                }
                break;

            case CardRevealed e:
                var gameReveal = await context.GameViews.FindAsync(new object[] { e.GameId }, ct);
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
        }
    }

    private static List<string> GetMarketForLevel(GameView game, int level)
    {
        return level switch
        {
            1 => game.Market1,
            2 => game.Market2,
            3 => game.Market3,
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
