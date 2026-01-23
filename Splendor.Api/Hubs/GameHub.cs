using Microsoft.AspNetCore.SignalR;

namespace Splendor.Api.Hubs;

public class GameHub : Hub
{
    // Player joins the game "room"
    public async Task JoinGame(Guid gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
    }

    // Player leaves the "room"
    public async Task LeaveGame(Guid gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());
    }
}
