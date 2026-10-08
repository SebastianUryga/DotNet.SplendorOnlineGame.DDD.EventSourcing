using System.Security.Claims;
using Splendor.Api.Auth;
using Splendor.Application.Common.Interfaces;
using Splendor.Domain.Rules;

namespace Splendor.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public Caller? Caller => string.IsNullOrEmpty(UserId)
        ? null
        : new Caller(Splendor.Domain.ValueObjects.UserId.Create(UserId), User!.FindAll(RoleClaims.Type).Select(c => c.Value).ToHashSet());
}
