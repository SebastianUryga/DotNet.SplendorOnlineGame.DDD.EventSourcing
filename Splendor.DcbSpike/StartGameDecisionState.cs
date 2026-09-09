using JasperFx.Events.Aggregation;
using JasperFx.Events.Tags;

namespace Splendor.DcbSpike;

[BoundaryAggregate]
internal class StartGameDecisionState
{
    public string? CreatorId { get; private set; }
    public GameStatus Status { get; private set; }
    public bool Started => Status == GameStatus.Started;
    public List<string> PlayerOrder { get; } = new();
    public Dictionary<string, PlayerState> Players { get; } = new();

    public static EventTagQuery Query(Guid gameId) =>
        new EventTagQuery()
            .Or<GameCreated, GameTag>(new GameTag(gameId))
            .Or<PlayerJoined, GameTag>(new GameTag(gameId))
            .Or<GameStarted, GameTag>(new GameTag(gameId))
            .Or<GameFinished, GameTag>(new GameTag(gameId))
            .Or<GameDeleted, GameTag>(new GameTag(gameId));

    public void Apply(GameCreated e)
    {
        CreatorId = e.CreatorId;
        Status = GameStatus.Created;
    }

    public void Apply(PlayerJoined e)
    {
        if (!Players.ContainsKey(e.PlayerId))
        {
            PlayerOrder.Add(e.PlayerId);
        }

        Players[e.PlayerId] = new PlayerState(e.OwnerId, e.Name);
    }

    public void Apply(GameStarted _)
    {
        Status = GameStatus.Started;
    }

    public void Apply(GameFinished _)
    {
        Status = GameStatus.Finished;
    }

    public void Apply(GameDeleted _)
    {
        Status = GameStatus.Deleted;
    }
}
