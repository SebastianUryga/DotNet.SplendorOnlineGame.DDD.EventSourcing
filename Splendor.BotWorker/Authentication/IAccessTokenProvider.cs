namespace Splendor.BotWorker.Authentication;

public interface IAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);

    Task<string> GetUserIdAsync(CancellationToken cancellationToken);
}
