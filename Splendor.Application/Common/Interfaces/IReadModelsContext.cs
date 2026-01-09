using Microsoft.EntityFrameworkCore;
using Splendor.Application.ReadModels;

namespace Splendor.Application.Common.Interfaces;

public interface IReadModelsContext
{
    DbSet<GameView> GameViews { get; }
    DbSet<PlayerView> PlayerViews { get; }
}
