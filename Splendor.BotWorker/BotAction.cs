namespace Splendor.BotWorker;

public abstract record BotAction;

public sealed record TakeGemsAction(int Diamond, int Sapphire, int Emerald, int Ruby, int Onyx, int Gold) : BotAction;

public sealed record BuyCardAction(string CardId) : BotAction;

public sealed record ReserveCardAction(string CardId) : BotAction;

public sealed record ResolveGemLimitAction(int Diamond, int Sapphire, int Emerald, int Ruby, int Onyx, int Gold) : BotAction;

public sealed record ChooseNobleAction(string NobleId) : BotAction;