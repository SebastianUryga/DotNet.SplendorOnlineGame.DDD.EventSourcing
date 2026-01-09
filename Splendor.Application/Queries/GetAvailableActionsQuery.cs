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
        // Note: Ideally we should query the ReadModel (GameView) instead of loading the Aggregate, 
        // but for logic checks (IsStarted, Players count) the Aggregate or a detailed ReadModel is fine.
        // Using Aggregate here to ensure consistency with domain logic.
        var game = await _eventStore.LoadAsync<Game>(request.GameId, cancellationToken);
        if (game == null) return null;

        var actions = new List<string>();
        if (!game.IsStarted) 
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
