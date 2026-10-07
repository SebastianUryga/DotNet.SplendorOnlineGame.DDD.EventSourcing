using System.ComponentModel.DataAnnotations;

namespace Splendor.Contracts.Games;

public record TakeGemsRequest(
    [StringLength(128)] string PlayerId,
    int Diamond,
    int Sapphire,
    int Emerald,
    int Ruby,
    int Onyx,
    int Gold);

public record BuyCardRequest(
    [StringLength(128)] string PlayerId,
    [StringLength(128)] string CardId);

// CardId reserves a visible market card; null CardId with Level reserves blindly from a deck.
public record ReserveCardRequest(
    [StringLength(128)] string PlayerId,
    [StringLength(128)] string? CardId,
    int? Level);

public record ResolveGemLimitRequest(
    [StringLength(128)] string PlayerId,
    int Diamond,
    int Sapphire,
    int Emerald,
    int Ruby,
    int Onyx,
    int Gold);

public record ChooseNobleRequest(
    [StringLength(128)] string PlayerId,
    [StringLength(128)] string NobleId);

public record CreateGameRequest();
public record JoinGameRequest([StringLength(20, MinimumLength = 1)] string Name);
public record InvitePlayerRequest(
    [StringLength(128)] string InviteeId);
