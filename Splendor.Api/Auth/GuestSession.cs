namespace Splendor.Api.Auth;

public class GuestSession
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
