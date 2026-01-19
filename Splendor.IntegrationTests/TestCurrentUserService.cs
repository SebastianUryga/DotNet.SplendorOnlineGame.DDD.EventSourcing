using Splendor.Application.Common.Interfaces;

namespace Splendor.IntegrationTests;

public class TestCurrentUserService : ICurrentUserService
{
    public string? UserId => "test-user-id";
}
