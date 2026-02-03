using MediatR;
using Microsoft.EntityFrameworkCore;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Application.Queries;

public record GetGameVersionQuery(Guid GameId) : IRequest<long?>;

public class GetGameVersionQueryHandler : IRequestHandler<GetGameVersionQuery, long?>
{
    private readonly IReadModelsContext _context;

    public GetGameVersionQueryHandler(IReadModelsContext context)
    {
        _context = context;
    }

    public async Task<long?> Handle(GetGameVersionQuery request, CancellationToken cancellationToken)
    {
        var version = await _context.GameViews
            .Where(x => x.Id == request.GameId)
            .Select(x => (long?)x.Version)
            .FirstOrDefaultAsync(cancellationToken);
            
        return version;
    }
}
