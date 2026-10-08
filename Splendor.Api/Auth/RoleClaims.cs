namespace Splendor.Api.Auth;

public static class RoleClaims
{
    // Namespaced custom claim: Auth0 adds it through a Login Action, guest tokens carry it too.
    public const string Type = "https://splendoronlinegame-web.onrender.com/roles";
}
