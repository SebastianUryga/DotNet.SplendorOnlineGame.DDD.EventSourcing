namespace Splendor.BotWorker;

public interface IBotGameMembershipHandler
{
    Task HandleInvitationAsync(Guid gameId, CancellationToken cancellationToken);
}
