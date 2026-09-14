using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Splendor.Application.Events;
using Splendor.Domain.Common;
using Splendor.Domain.Events;
using Splendor.Infrastructure.Events;
using Testcontainers.PostgreSql;

namespace Splendor.IntegrationTests;

public class SplendorApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove real database
            services.RemoveAll(typeof(IDocumentStore));


            // Add Test PostgreSql (Marten)
            services.AddMarten((StoreOptions options) =>
            {
                options.Connection(_postgreSqlContainer.GetConnectionString());
                options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;

                // Events configuration
                options.Events.StreamIdentity = StreamIdentity.AsGuid;
                options.Events.RegisterTagType<GameTag>("game");
                options.Events.RegisterTagType<OwnerTag>("owner");
                options.Events.TagEventsBy(@event => @event switch
                {
                    GameCreated created => [new GameTag(created.GameId), new OwnerTag(created.CreatorId)],
                    PlayerJoined joined => [new GameTag(joined.GameId), new OwnerTag(joined.OwnerId)],
                    IDomainEvent domainEvent => [new GameTag(domainEvent.GameId)],
                    _ => []
                });

                options.Projections.Add<Splendor.Infrastructure.Projections.GameSummaryProjection>(ProjectionLifecycle.Inline);
                options.Projections.Add<Splendor.Infrastructure.Projections.SplendorBoardProjection>(ProjectionLifecycle.Inline);
            }).UseLightweightSessions();

            // services.RemoveAll(typeof(Splendor.Application.Common.Interfaces.ICurrentUserService));
            // services.AddScoped<Splendor.Application.Common.Interfaces.ICurrentUserService, TestCurrentUserService>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
    }

    public HttpClient CreateAuthenticatedClient()
    {
        return CreateDefaultClient(new TestUserContext.TestUserDelegatingHandler());
    }
}
