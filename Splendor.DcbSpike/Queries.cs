using Marten;

namespace Splendor.DcbSpike;

public class GetGamesQueryHandler
{
    private readonly IQuerySession _session;

    public GetGamesQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public Task<IReadOnlyList<GameSummaryView>> Handle(bool includeDeleted, CancellationToken cancellationToken) =>
        _session.Query<GameSummaryView>()
            .Where(x => includeDeleted || x.Status != "Deleted")
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
}

public class GetSplendorBoardQueryHandler
{
    private readonly IQuerySession _session;

    public GetSplendorBoardQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public Task<SplendorBoardView?> Handle(Guid gameId, CancellationToken cancellationToken) =>
        _session.LoadAsync<SplendorBoardView>(gameId, cancellationToken);
}

public class GetUserStatsQueryHandler
{
    private readonly IQuerySession _session;

    public GetUserStatsQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public Task<UserStatsView?> Handle(string ownerId, CancellationToken cancellationToken) =>
        _session.LoadAsync<UserStatsView>(ownerId, cancellationToken);
}
