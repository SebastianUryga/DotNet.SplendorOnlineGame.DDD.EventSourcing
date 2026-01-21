using Microsoft.EntityFrameworkCore;
using Splendor.Application.ReadModels;
using Splendor.Domain.ValueObjects;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Infrastructure.Persistence;

public class ReadModelsContext : DbContext, IReadModelsContext
{
    public DbSet<GameView> GameViews { get; set; }
    public DbSet<PlayerView> PlayerViews { get; set; }

    public ReadModelsContext(DbContextOptions<ReadModelsContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameView>(b =>
        {
            b.HasKey(x => x.Id);
            b.OwnsOne(x => x.MarketGems); // Embed GemCollection as columns
            b.HasMany(x => x.Players).WithOne(); // Simple relation

            b.Property(x => x.Market1)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());

            b.Property(x => x.Market2)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());

            b.Property(x => x.Market3)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());
        });

        modelBuilder.Entity<PlayerView>(b =>
        {
            b.HasKey(x => x.Id);
            b.OwnsOne(x => x.Gems); // Embed GemCollection

            b.Property(x => x.OwnedCardIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());
        });
        
        base.OnModelCreating(modelBuilder);
    }
}
