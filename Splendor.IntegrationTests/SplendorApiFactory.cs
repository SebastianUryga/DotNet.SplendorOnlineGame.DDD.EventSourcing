using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Splendor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Marten;
using Microsoft.AspNetCore.Authentication;
using MassTransit;

namespace Splendor.IntegrationTests;

public class SplendorApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .Build();

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Splendor!123")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove real databases
            services.RemoveAll(typeof(DbContextOptions<ReadModelsContext>));
            services.RemoveAll(typeof(IDocumentStore));

            // Add Test MsSql (EF Core)
            services.AddDbContext<ReadModelsContext>(options =>
                options.UseSqlServer(_msSqlContainer.GetConnectionString() + ";TrustServerCertificate=True;"));

            // Add Test PostgreSql (Marten)
            services.AddMarten(options =>
            {
                options.Connection(_postgreSqlContainer.GetConnectionString());
                options.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
                options.Events.StreamIdentity = Marten.Events.StreamIdentity.AsGuid;
                options.Projections.Add<Splendor.Infrastructure.Projections.GameProjection>(Marten.Events.Projections.ProjectionLifecycle.Inline);
            }).UseLightweightSessions();

            // Bypass Auth using TestAuthHandler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            services.RemoveAll(typeof(Splendor.Application.Common.Interfaces.ICurrentUserService));
            services.AddScoped<Splendor.Application.Common.Interfaces.ICurrentUserService, TestCurrentUserService>();

            // MassTransit - InMemory for tests (no RabbitMQ needed)
            services.AddMassTransit(x =>
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _msSqlContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
        await _msSqlContainer.StopAsync();
    }
}
