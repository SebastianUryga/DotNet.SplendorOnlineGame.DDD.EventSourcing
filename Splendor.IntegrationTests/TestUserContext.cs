using Splendor.Application.Common.Interfaces;

namespace Splendor.IntegrationTests;

public class TestUserContext
{
    private static readonly AsyncLocal<string?> _currentUserId = new();

    public static string DefaultUserId => "test-user-id";

    public static IDisposable SetUser(string userId)
    {
        _currentUserId.Value = userId;
        return new UserScope();
    }

    private class UserScope : IDisposable
    {
        public void Dispose() => _currentUserId.Value = null;
    }

    public class TestUserDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var userId = _currentUserId.Value ?? DefaultUserId;
            request.Headers.Add("X-Test-User-Id", userId);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
