using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Splendor.Api.Hubs;
using Splendor.Application.Queries;
using MediatR;
using Splendor.Contracts.Messages;
using Splendor.Domain.ValueObjects;

namespace Splendor.Api.Consumers;

public record GameNotification(string Type, string? PlayerId, object? Data = null);

public class GameUpdatedConsumer : IConsumer<Batch<GameUpdatedMessage>>
{
    // Only these events reach clients, with a whitelisted subset of fields (no deck contents etc.).
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

    private static readonly JsonSerializerOptions EventJson = new() { PropertyNameCaseInsensitive = true };

    // Data is built by hand per event type (whitelist): a new event field never leaks to clients.
    // card_reserved carries no card id: a blind reservation from a deck would reveal the hidden card.
    public static GameNotification ToNotification(GameUpdatedMessage message)
    {
        if (message.Data == null) return new GameNotification(message.EventType, null);

        using var doc = JsonDocument.Parse(message.Data);
        var root = doc.RootElement;
        var playerId = root.TryGetProperty("PlayerId", out var p) ? p.GetString() : null;

        object? data = message.EventType switch
        {
            "card_purchased" => new
            {
                CardId = root.TryGetProperty("CardId", out var c) ? c.GetString() : null,
                PaidGems = root.TryGetProperty("PaidGems", out var g) ? g.Deserialize<GemCollection>(EventJson) : null
            },
            "gems_taken" => new
            {
                Gems = root.TryGetProperty("Gems", out var g) ? g.Deserialize<GemCollection>(EventJson) : null
            },
            "noble_acquired" => new
            {
                NobleId = root.TryGetProperty("NobleId", out var n) ? n.GetString() : null
            },
            _ => null
        };

        return new GameNotification(message.EventType, playerId, data);
    }
}
