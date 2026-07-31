using MediatR;

namespace Splendor.Application.Behaviors;

public class EventStoreRetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int MaxAttempts = 3;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await next();
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                lastException = ex;
                if (attempt == MaxAttempts)
                {
                    break;
                }

                // small backoff before retry
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("EventStoreRetryBehavior failed without exception.");
    }

    private static bool IsTransient(Exception ex)
    {
        if (ex == null) return false;

        var name = ex.GetType().Name;
        if (name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Transient", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Timeout", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Deadlock", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Unavailable", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("UnexpectedMaxEventId", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("WrongExpectedVersion", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ex.InnerException != null && IsTransient(ex.InnerException);
    }
}