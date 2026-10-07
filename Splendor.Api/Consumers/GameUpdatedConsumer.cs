using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Splendor.Api.Hubs;
using Splendor.Application.Queries;
using MediatR;
using Splendor.Contracts.Messages;

namespace Splendor.Api.Consumers;

public record GameNotification(string Type, string? PlayerId);

public class GameUpdatedConsumer : IConsumer<Batch<GameUpdatedMessage>>
{
    // Only these events reach clients, and only their type and acting player (no deck contents etc.).
    private static readonly HashSet<string> NotifiableEvents =
        ["gems_taken", "card_purchased", "card_reserved", "noble_acquired", "turn_expired"];

    private readonly IHubContext<GameHub> _hubContext;
    private readonly IMediator _mediator;

    public GameUpdatedConsumer(IHubContext<GameHub> hubContext, IMediator mediator)
    {
        _hubContext = hubContext;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<Batch<GameUpdatedMessage>> context)
    {
        var byGame = context.Message.Select(m => m.Message).GroupBy(m => m.GameId);

        foreach (var messages in byGame)
        {
            var gameView = await _mediator.Send(new GetGameQuery(messages.Key));
            if (gameView == null) continue;

            var notifications = messages
                .Where(m => NotifiableEvents.Contains(m.EventType))
                .OrderBy(m => m.StreamVersion)
                .Select(ToNotification)
                .ToList();

            await _hubContext.Clients
                .Group(messages.Key.ToString())
                .SendAsync("GameUpdated", gameView, notifications);
        }
    }

    private static GameNotification ToNotification(GameUpdatedMessage message)
    {
        string? playerId = null;
        if (message.Data != null)
        {
            using var doc = JsonDocument.Parse(message.Data);
            if (doc.RootElement.TryGetProperty("PlayerId", out var p)) playerId = p.GetString();
        }
        return new GameNotification(message.EventType, playerId);
    }
}
