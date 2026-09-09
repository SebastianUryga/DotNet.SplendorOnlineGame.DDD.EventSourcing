using JasperFx.Events.Tags;
using Marten;
using Splendor.DcbSpike.ValueObjects;

namespace Splendor.DcbSpike;

public record StartGameCommand(Guid GameId, string OwnerId);

public class StartGameCommandHandler
{
    private readonly IDocumentSession _session;

    public StartGameCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task Handle(StartGameCommand command, CancellationToken cancellationToken)
    {
        var query = new EventTagQuery()
            .Or<GameCreated, GameTag>(new GameTag(command.GameId))
            .Or<PlayerJoined, GameTag>(new GameTag(command.GameId))
            .Or<GameStarted, GameTag>(new GameTag(command.GameId))
            .Or<GameFinished, GameTag>(new GameTag(command.GameId))
            .Or<GameDeleted, GameTag>(new GameTag(command.GameId));

        var boundary = await _session.Events.FetchForWritingByTags<StartGameDecisionState>(query, cancellationToken);
        var state = boundary.Aggregate ?? throw new InvalidOperationException("Game not found.");

        var events = Decide(command, state);

        boundary.AppendMany(events.Select(e => _session.Tag(e)).ToArray());
        await _session.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<IDomainEvent> Decide(StartGameCommand command, StartGameDecisionState state)
    {
        if (state.Status == GameStatus.Deleted) throw new InvalidOperationException("Game deleted.");
        if (state.Status == GameStatus.Finished) throw new InvalidOperationException("Game finished.");
        if (state.Started) throw new InvalidOperationException("Game already started.");
        if (state.Status != GameStatus.Created) throw new InvalidOperationException("Game not created.");
        if (state.PlayerOrder.Count < 2) throw new InvalidOperationException("Need at least 2 players.");
        if (state.PlayerOrder.Count > 4) throw new InvalidOperationException("Too many players.");

        var isCreator = state.CreatorId == command.OwnerId;
        var isParticipant = state.Players.Values.Any(p => p.OwnerId == command.OwnerId);

        if (!isCreator && !isParticipant)
        {
            throw new InvalidOperationException("Only the creator or a participant can start the game.");
        }

        var now = DateTimeOffset.UtcNow;
        var level1 = CardCatalog.GetCardIdsByLevel(1);
        var level2 = CardCatalog.GetCardIdsByLevel(2);
        var level3 = CardCatalog.GetCardIdsByLevel(3);

        return
        [
            new GameStarted(
                command.GameId,
                StartingMarketGems(state.PlayerOrder.Count),
                level1.Skip(4).ToList(),
                level2.Skip(4).ToList(),
                level3.Skip(4).ToList(),
                level1.Take(4).ToList(),
                level2.Take(4).ToList(),
                level3.Take(4).ToList(),
                [],
                now),
            new TurnStarted(command.GameId, state.PlayerOrder[0], now)
        ];
    }

    public static GemCollection StartingMarketGems(int playerCount)
    {
        var regularGems = playerCount switch
        {
            2 => 4,
            3 => 5,
            4 => 7,
            _ => throw new ArgumentOutOfRangeException(nameof(playerCount), "Splendor supports 2-4 players.")
        };

        return new GemCollection(regularGems, regularGems, regularGems, regularGems, regularGems, 5);
    }
}
