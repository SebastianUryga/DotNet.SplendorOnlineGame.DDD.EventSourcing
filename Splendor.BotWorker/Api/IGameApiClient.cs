using Splendor.Contracts.Games;
using Splendor.Domain.ValueObjects;

namespace Splendor.BotWorker.Api;

public interface IGameApiClient
{
    Task<GameView?> GetGameAsync(Guid gameId, CancellationToken cancellationToken);

    Task TakeGemsAsync(Guid gameId, TakeGemsRequest request, CancellationToken cancellationToken);

    Task BuyCardAsync(Guid gameId, BuyCardRequest request, CancellationToken cancellationToken);

    Task ReserveCardAsync(Guid gameId, ReserveCardRequest request, CancellationToken cancellationToken);

    Task ResolveGemLimitAsync(Guid gameId, ResolveGemLimitRequest request, CancellationToken cancellationToken);

    Task ChooseNobleAsync(Guid gameId, ChooseNobleRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<Card>> GetCardsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Noble>> GetNoblesAsync(CancellationToken cancellationToken);

    Task JoinGameAsync(Guid gameId, JoinGameRequest request, CancellationToken cancellationToken);
}