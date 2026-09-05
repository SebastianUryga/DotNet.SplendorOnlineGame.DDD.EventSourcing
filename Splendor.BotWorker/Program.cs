using MassTransit;
using Splendor.BotWorker.Api;
using Splendor.BotWorker.Authentication;
using Splendor.BotWorker.Cards;
using Splendor.BotWorker.Messaging;
using Splendor.BotWorker.Processing;
using Splendor.BotWorker.Strategies;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient<IGameApiClient, GameApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
});

builder.Services.AddSingleton<IAccessTokenProvider, Auth0AccessTokenProvider>();
builder.Services.AddHttpClient<CardDefinitionsProvider>();
builder.Services.AddSingleton<ICardDefinitionsProvider, CardDefinitionsProvider>();
builder.Services.AddScoped<IBotTurnProcessor, BotTurnProcessor>();
builder.Services.AddScoped<IBotStrategy, GreedyBotStrategy>();

builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<BotGameUpdatedConsumer>(c =>
    {
        c.Options<BatchOptions>(o =>
        {
            o.MessageLimit = 100;
            o.TimeLimit = TimeSpan.FromMilliseconds(100);
        });
    });

    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMq:Host"]!,
            builder.Configuration["RabbitMq:VirtualHost"] ?? "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
            });

        cfg.ReceiveEndpoint("splendor-bot-game-updated", endpoint =>
        {
            endpoint.ConfigureConsumer<BotGameUpdatedConsumer>(context);
        });
    });
});

await builder.Build().RunAsync();