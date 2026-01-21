using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Splendor.Application.ReadModels;
using Splendor.Domain.ValueObjects;
using Splendor.Application.Common.Interfaces;
using System.Text.Json;

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
        // ValueComparer for List<string> - enables EF Core to detect in-place mutations
        var stringListComparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()
        );

        modelBuilder.Entity<GameView>(b =>
        {
            b.HasKey(x => x.Id);
            b.OwnsOne(x => x.MarketGems);
            b.HasMany(x => x.Players).WithOne();

            b.Property(x => x.Market1)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);

            b.Property(x => x.Market2)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);

            b.Property(x => x.Market3)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<PlayerView>(b =>
        {
            b.HasKey(x => x.Id);
            b.OwnsOne(x => x.Gems);

            b.Property(x => x.OwnedCardIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(stringListComparer);
        });

        base.OnModelCreating(modelBuilder);
    }
}
