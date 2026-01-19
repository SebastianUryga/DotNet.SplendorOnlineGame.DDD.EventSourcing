using MediatR;
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
        var game = await _context.GameViews.FindAsync(new object[] { request.GameId }, cancellationToken);
        return game?.Version;
    }
}
