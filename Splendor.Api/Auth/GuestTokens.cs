using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Splendor.Api.Auth;

public static class GuestTokens
{
    public const string Scheme = "Guest";
    public const string Issuer = "splendor-guest";

    public static TokenValidationParameters Validation(string key) => new()
    {
        ValidIssuer = Issuer,
        ValidateAudience = false,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    public static string Create(string key, string guestId, DateTime expires) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Claims = new Dictionary<string, object> { ["sub"] = guestId },
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256)
        });

    public static bool IsGuestToken(string token)
    {
        try { return new JsonWebToken(token).Issuer == Issuer; }
        catch { return false; }
    }
}
