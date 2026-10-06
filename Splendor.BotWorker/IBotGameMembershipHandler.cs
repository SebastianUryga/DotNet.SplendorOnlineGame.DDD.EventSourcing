namespace Splendor.BotWorker;

public interface IBotGameMembershipHandler
{
    Task HandleInvitationAsync(Guid gameId, string? eventData, CancellationToken cancellationToken);
}
