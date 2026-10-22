namespace Splendor.BotWorker;

public class BotOptions
{
    // Pause before the bot makes its move, so client animations can finish; 0 disables it (load tests)
    public int MoveDelayMs { get; set; } = 2000;
}
