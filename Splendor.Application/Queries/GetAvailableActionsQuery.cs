using MediatR;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Aggregates;

namespace Splendor.Application.Queries;

public record GetAvailableActionsQuery(Guid GameId) : IRequest<List<string>?>;

public class GetAvailableActionsQueryHandler : IRequestHandler<GetAvailableActionsQuery, List<string>?>
{
    private readonly IEventStore _eventStore;

    public GetAvailableActionsQueryHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<List<string>?> Handle(GetAvailableActionsQuery request, CancellationToken cancellationToken)
    {
        var game = await _eventStore.LoadAsync<Game>(request.GameId, cancellationToken);
        if (game == null) return null;

        var actions = new List<string>();
        if (game.Status == "Created") 
        {
             if (game.Players.Count >= 2) actions.Add("StartGame");
             if (game.Players.Count < 4) actions.Add("JoinGame");
        }
        else 
        {
             actions.Add("TakeGems");
             actions.Add("BuyCard");
             actions.Add("ReserveCard");
        }
        
        return actions;
    }
}
