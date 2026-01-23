using Splendor.Application.Common.Interfaces;

namespace Splendor.IntegrationTests;

public class TestCurrentUserService : ICurrentUserService
{
    private static readonly AsyncLocal<string?> _currentUserId = new();

    public static string DefaultUserId => "test-user-id";

    public string? UserId => _currentUserId.Value ?? DefaultUserId;

    public static IDisposable SetUser(string userId)
    {
        _currentUserId.Value = userId;
        return new UserScope();
    }

    private class UserScope : IDisposable
    {
        public void Dispose() => _currentUserId.Value = null;
    }
}
