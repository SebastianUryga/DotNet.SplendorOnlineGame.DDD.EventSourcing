namespace Splendor.Application.Common.Interfaces;

public interface IAuthoredCommand
{
    string OwnerId { get; init; }
}
