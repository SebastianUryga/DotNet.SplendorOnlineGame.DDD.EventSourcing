using Splendor.Domain.ValueObjects;

namespace Splendor.Domain.Rules;

/// <summary>Who invokes a command and with which roles (the "headers" of a command).</summary>
public record Caller(UserId UserId, IReadOnlySet<string> Roles)
{
    public const string AdminRole = "admin";
    public const string GuestRole = "guest";

    public bool IsAdmin => Roles.Contains(AdminRole);
    public bool IsGuest => Roles.Contains(GuestRole);

    public static readonly Caller System = User("system", AdminRole);

    public static Caller User(string userId, params string[] roles) =>
        new(UserId.Create(userId), roles.ToHashSet());
}
