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

public record ReserveCardRequest(
    string PlayerId,
    string CardId);

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