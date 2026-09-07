namespace Splendor.BotWorker.Messaging
{
    public static class GameEventTypes {
        public const string GameCreated = "game_created";
        public const string PlayerJoined = "player_joined";
        public const string PlayerInvited = "player_invited";
        public const string GameStarted = "game_started";
        public const string TurnStarted = "turn_started";
        public const string GemsTaken = "gems_taken";
        public const string GemsOverflowDetected = "gems_overflow_detected";
        public const string GemLimitResolved = "gem_limit_resolved";
        public const string TurnEnded = "turn_ended";
        public const string CardPurchased = "card_purchased";
        public const string CardRevealed = "card_revealed";
        public const string CardReserved = "card_reserved";
        public const string NobleSelectionRequired = "noble_selection_required";
        public const string NobleAcquired = "noble_acquired";
        public const string GameFinished = "game_finished";
        public const string GameDeleted = "game_deleted"; }
}
