using MediatR;
using Marten;
using Splendor.Application.ReadModels;

namespace Splendor.Application.Queries;

public record GetGamesQuery(bool IncludeDeleted = false) : IRequest<IEnumerable<GameSummaryDto>>;

public class GetGamesQueryHandler : IRequestHandler<GetGamesQuery, IEnumerable<GameSummaryDto>>
{
    private readonly IQuerySession _session;

    public GetGamesQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public async Task<IEnumerable<GameSummaryDto>> Handle(GetGamesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<GameSummaryView> query = _session.Query<GameSummaryView>();
        
        if (!request.IncludeDeleted)
        {
            query = query.Where(g => g.Status != "Deleted");
        }

        return await query
            .Select(g => new GameSummaryDto(g.Id, g.Status, g.PlayerCount))
            .ToListAsync(cancellationToken);
    }
}
