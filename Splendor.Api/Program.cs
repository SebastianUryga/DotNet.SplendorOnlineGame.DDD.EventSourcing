using Marten;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Splendor.Api.Consumers;
using Splendor.Api.Hubs;
using Splendor.Application;
using Splendor.Application.Snapshots;
using Splendor.Infrastructure;
using Splendor.Infrastructure.Projections;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"];
var openTelemetry = builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Splendor.Api"))
    .WithMetrics(metrics => metrics
        .AddMeter("Splendor.Application")
        .AddMeter("Splendor.Infrastructure")
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
        .AddMeter("System.Net.Http"))
    .WithTracing(tracing => tracing
        .AddSource("Splendor.Application")
        .AddSource("Splendor.Infrastructure")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var otlpUri))
{
    openTelemetry
        .WithMetrics(metrics => metrics.AddOtlpExporter(options => options.Endpoint = otlpUri))
        .WithTracing(tracing => tracing.AddOtlpExporter(options => options.Endpoint = otlpUri));
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());

});
builder.Services.AddSignalR();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Splendor API",
        Version = "v1",
        Description = "An API for playing the Splendor board game, featuring event sourcing and real-time read models."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Marten") ?? string.Empty);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services
        .AddAuthentication("Test")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Splendor.Api.Testing.TestAuthHandler>("Test", _ => { });
}
else
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
            options.Audience = builder.Configuration["Auth0:Audience"];
        });
}

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddMassTransit(x =>
    {
        x.AddDelayedMessageScheduler();
        x.AddConsumer<GameUpdatedConsumer>();
        x.AddConsumer<ExpireTurnConsumer>();
        x.UsingInMemory((context, cfg) =>
        {
            cfg.UseDelayedMessageScheduler();
            cfg.ConfigureEndpoints(context);
        });
    });
}
else
{
    builder.Services.AddMassTransit(x =>
    {
        x.AddDelayedMessageScheduler();
        x.AddConsumer<ExpireTurnConsumer>();
        x.AddConsumer<GameUpdatedConsumer>(c =>
        {
            c.Options<BatchOptions>(o =>
            {
                o.MessageLimit = 100;
                o.TimeLimit = TimeSpan.FromMilliseconds(100);
            });
        });

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.UseDelayedMessageScheduler();
            var rabbitUrl = builder.Configuration["RabbitMq:Url"];
            if (rabbitUrl is null)
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
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

            cfg.ConfigureEndpoints(context);
        });
    });
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Splendor.Application.Common.Interfaces.ICurrentUserService, Splendor.Api.Services.CurrentUserService>();

var app = builder.Build();

app.UseMiddleware<Splendor.Api.Middleware.ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapPost(
        "/dev/projections/splendor-board/rebuild",
        async (IDocumentStore store, CancellationToken cancellationToken) =>
        {
            using var daemon = await store.BuildProjectionDaemonAsync();

            await daemon.RebuildProjectionAsync<SplendorBoardProjection>(
                cancellationToken);

            return Results.NoContent();
        });
    app.MapPost(
    "/dev/projections/game-state/rebuild",
    async (IDocumentStore store, CancellationToken cancellationToken) =>
    {
        using var daemon = await store.BuildProjectionDaemonAsync();

        await daemon.RebuildProjectionAsync<SplendorGameState>(
            cancellationToken);

        return Results.NoContent();
    });
}

app.UseCors();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.Run();

public partial class Program { }
