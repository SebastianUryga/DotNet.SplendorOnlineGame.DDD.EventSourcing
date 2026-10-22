using System.Text.Json;
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

    private record Invitation(string? InviterId, string? InviteeId);

    private static Invitation? ReadInvitation(string? eventData) =>
        string.IsNullOrEmpty(eventData) ? null : JsonSerializer.Deserialize<Invitation>(eventData);

    public async Task HandleInvitationAsync(Guid gameId, string? eventData, CancellationToken cancellationToken)
    {
        var botUserId = await _accessTokenProvider.GetUserIdAsync(cancellationToken);
        var invitation = ReadInvitation(eventData);
        if (invitation?.InviteeId != botUserId)
        {
            return;
        }

        var game = await _gameApiClient.GetGameAsync(gameId, cancellationToken);

        if (game is null)
        {
            _logger.LogWarning("Game {GameId} not available", gameId);
            return;
        }

        if (game.Status == "Created" && !game.Players.Any(p => p.OwnerId == botUserId))
        {
            // An inviter who is not a player is only watching, so the bot plays both seats
            var names = game.Players.Any(p => p.OwnerId == invitation.InviterId) ? new[] { "Bot-Player" } : new[] { "Bot-A", "Bot-B" };
            try
            {
                foreach (var name in names)
                {
                    await _gameApiClient.JoinGameAsync(gameId, new JoinGameRequest(name), cancellationToken);
                }
                _logger.LogInformation("Successfully joined game {GameId} with {Seats} seat(s)", gameId, names.Length);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while handling invitation for game {GameId}", gameId);
            }
        }
    }
}
