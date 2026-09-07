using Splendor.BotWorker.Api;
using Splendor.BotWorker.Authentication;
using Splendor.Contracts.Games;

namespace Splendor.BotWorker;

public class BotGameMembershipHandler : IBotGameMembershipHandler
{
    private readonly IGameApiClient _gameApiClient;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILogger<BotGameMembershipHandler> _logger;
    public BotGameMembershipHandler(IGameApiClient gameApiClient, IAccessTokenProvider accessTokenProvider, ILogger<BotGameMembershipHandler> logger)
    {
        _gameApiClient = gameApiClient;
        _accessTokenProvider = accessTokenProvider;
        _logger = logger;
    }

    public async Task HandleInvitationAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var botUserId = await _accessTokenProvider.GetUserIdAsync(cancellationToken);
        var game = await _gameApiClient.GetGameAsync(gameId, cancellationToken);

        if (game is null)
        {
            _logger.LogWarning("Game {GameId} not available", gameId);
            return;
        }

        if (game.Status == "Created" && !game.Players.Any(p => p.OwnerId == botUserId))
        {
            try
            {
                await _gameApiClient.JoinGameAsync(gameId, new JoinGameRequest("Bot-Player"), cancellationToken);
                _logger.LogInformation("Successfully joined game {GameId}", gameId);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while handling invitation for game {GameId}", gameId);
            }
        }
    }
}
