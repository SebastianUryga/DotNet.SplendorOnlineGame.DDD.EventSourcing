using MediatR;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Application.Queries;

public record GetGameHistoryQuery(Guid GameId) : IRequest<IReadOnlyList<object>?>;

public class GetGameHistoryQueryHandler : IRequestHandler<GetGameHistoryQuery, IReadOnlyList<object>?>
{
    private readonly IEventStore _eventStore;

    public GetGameHistoryQueryHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<IReadOnlyList<object>?> Handle(GetGameHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _eventStore.FetchStreamAsync(request.GameId, cancellationToken);
    }
}
