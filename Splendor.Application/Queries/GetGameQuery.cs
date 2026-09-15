using MediatR;
using Marten;
using Splendor.Application.ReadModels;

namespace Splendor.Application.Queries;

public record GetGameQuery(Guid GameId) : IRequest<GameView?>;

public class GetGameQueryHandler : IRequestHandler<GetGameQuery, GameView?>
{
    private readonly IQuerySession _session;

    public GetGameQueryHandler(IQuerySession session)
    {
        _session = session;
    }

    public async Task<GameView?> Handle(GetGameQuery request, CancellationToken cancellationToken)
    {
        var board = await _session.LoadAsync<SplendorBoardView>(request.GameId, cancellationToken);
        return board is null ? null : ToGameView(board);
    }

    private static GameView ToGameView(SplendorBoardView board) => new()
    {
        Id = board.Id,
        Version = board.GameVersion,
        Status = board.Status,
        Players = board.Players.Select(player => new PlayerView
        {
            Id = player.Id,
            OwnerId = player.OwnerId,
            Name = player.Name,
            Gems = player.Gems,
            OwnedCardIds = player.OwnedCardIds.ToList(),
            ReservedCardIds = player.ReservedCardIds.ToList(),
            OwnedNobleIds = player.OwnedNobleIds.ToList(),
            PrestigePoints = player.PrestigePoints,
            GameViewId = board.Id
        }).ToList(),
        MarketGems = board.MarketGems,
        CurrentPlayerId = board.CurrentPlayerId,
        WinnerId = board.WinnerId,
        WinnerName = board.WinnerName,
        IsGemReturnPending = board.IsGemReturnPending,
        Market1 = board.Market1.ToList(),
        Market2 = board.Market2.ToList(),
        Market3 = board.Market3.ToList(),
        Nobles = board.Nobles.ToList(),
        PlayerIdAwaitingNobleSelection = board.PlayerIdAwaitingNobleSelection,
        EligibleNobleIds = board.EligibleNobleIds.ToList(),
        Deck1Count = board.Deck1Count,
        Deck2Count = board.Deck2Count,
        Deck3Count = board.Deck3Count
    };
}
