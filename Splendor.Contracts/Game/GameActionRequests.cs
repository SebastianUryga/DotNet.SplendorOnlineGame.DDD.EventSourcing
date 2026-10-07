namespace Splendor.Contracts.Games;

public record TakeGemsRequest(
    string PlayerId,
    int Diamond,
    int Sapphire,
    int Emerald,
    int Ruby,
    int Onyx,
    int Gold);

public record BuyCardRequest(
    string PlayerId,
    string CardId);

// CardId reserves a visible market card; null CardId with Level reserves blindly from a deck.
public record ReserveCardRequest(
    string PlayerId,
    string? CardId,
    int? Level);

public record ResolveGemLimitRequest(
    string PlayerId,
    int Diamond,
    int Sapphire,
    int Emerald,
    int Ruby,
    int Onyx,
    int Gold);

public record ChooseNobleRequest(
    string PlayerId,
    string NobleId);

public record CreateGameRequest();
public record JoinGameRequest(string Name);
public record InvitePlayerRequest(
    string InviteeId);
