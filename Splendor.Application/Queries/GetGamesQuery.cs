using MediatR;
using Microsoft.EntityFrameworkCore;
using Splendor.Application.ReadModels;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Application.Queries;

public record GetGamesQuery(bool IncludeDeleted = false) : IRequest<IEnumerable<GameSummaryDto>>;

public class GetGamesQueryHandler : IRequestHandler<GetGamesQuery, IEnumerable<GameSummaryDto>>
{
    private readonly IReadModelsContext _context;

    public GetGamesQueryHandler(IReadModelsContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GameSummaryDto>> Handle(GetGamesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.GameViews.AsQueryable();
        
        if (!request.IncludeDeleted)
        {
            query = query.Where(g => g.Status != "Deleted");
        }

        return await query
            .Select(g => new GameSummaryDto(g.Id, g.Status, g.Players.Count))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
