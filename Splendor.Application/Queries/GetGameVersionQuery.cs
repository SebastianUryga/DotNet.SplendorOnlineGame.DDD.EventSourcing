using MediatR;
using Marten;
using Splendor.Application.ReadModels;

namespace Splendor.Application.Queries;

public record GetGameVersionQuery(Guid GameId) : IRequest<long?>;

public class GetGameVersionQueryHandler : IRequestHandler<GetGameVersionQuery, long?>
{
    private readonly IQuerySession _session;

    public GetGameVersionQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public async Task<long?> Handle(GetGameVersionQuery request, CancellationToken cancellationToken)
    {
        var game = await _session.LoadAsync<SplendorBoardView>(request.GameId, cancellationToken);
        return game?.Version;
    }
}
