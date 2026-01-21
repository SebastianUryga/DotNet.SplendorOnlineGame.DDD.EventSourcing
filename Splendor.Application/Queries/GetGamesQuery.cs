using MediatR;
using Microsoft.EntityFrameworkCore;
using Splendor.Application.ReadModels;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Application.Queries;

public record GetGamesQuery : IRequest<IEnumerable<GameSummaryDto>>;

public class GetGamesQueryHandler : IRequestHandler<GetGamesQuery, IEnumerable<GameSummaryDto>>
{
    private readonly IReadModelsContext _context;

    public GetGamesQueryHandler(IReadModelsContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GameSummaryDto>> Handle(GetGamesQuery request, CancellationToken cancellationToken)
    {
        return await _context.GameViews
            .Select(g => new GameSummaryDto(g.Id, g.Status, g.Players.Count))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
