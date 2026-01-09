using MediatR;
using Microsoft.EntityFrameworkCore;
using Splendor.Application.ReadModels;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Application.Queries;

public record GetGameQuery(Guid GameId) : IRequest<GameView?>;

public class GetGameQueryHandler : IRequestHandler<GetGameQuery, GameView?>
{
    private readonly IReadModelsContext _context;

    public GetGameQueryHandler(IReadModelsContext context)
    {
        _context = context;
    }

    public async Task<GameView?> Handle(GetGameQuery request, CancellationToken cancellationToken)
    {
        return await _context.GameViews
            .Include(g => g.Players)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GameId, cancellationToken);
    }
}
