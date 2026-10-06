using MassTransit;
using Serilog;
using Splendor.BotWorker;
using Splendor.BotWorker.Api;
using Splendor.BotWorker.Authentication;
using Splendor.BotWorker.Cards;
using Splendor.BotWorker.Messaging;
using Splendor.BotWorker.Processing;
using Splendor.BotWorker.Strategies;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Splendor.BotWorker"));

builder.Services.AddHttpClient<IGameApiClient, GameApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
});

builder.Services.AddSingleton<IAccessTokenProvider, Auth0AccessTokenProvider>();
builder.Services.AddSingleton<IGameDefinitionsProvider, GameDefinitionsProvider>();
builder.Services.AddScoped<IBotGameMembershipHandler, BotGameMembershipHandler>();
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
        var rabbitUrl = builder.Configuration["RabbitMq:Url"];
        if (rabbitUrl is null)
        {
            cfg.Host(
                builder.Configuration["RabbitMq:Host"]!,
                builder.Configuration["RabbitMq:VirtualHost"] ?? "/",
                h =>
                {
                    h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
                    h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
                });
        }
        else
        {
            var uri = new Uri(rabbitUrl);
            var credentials = uri.UserInfo.Split(':', 2);
            cfg.Host(uri.Host, (ushort)(uri.Port > 0 ? uri.Port : 5671), uri.AbsolutePath.TrimStart('/'), h =>
            {
                h.Username(Uri.UnescapeDataString(credentials[0]));
                h.Password(Uri.UnescapeDataString(credentials[1]));
                if (uri.Scheme == "amqps")
                {
                    h.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                }
            });
        }
        cfg.ReceiveEndpoint(
            new TemporaryEndpointDefinition("bot-game-updated"),
            KebabCaseEndpointNameFormatter.Instance,
            endpoint =>
            {
                endpoint.ConfigureConsumer<BotGameUpdatedConsumer>(context);
            });
    });
});

await builder.Build().RunAsync();