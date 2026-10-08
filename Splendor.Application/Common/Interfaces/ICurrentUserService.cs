namespace Splendor.Application.Common.Interfaces;

using Splendor.Domain.Rules;

public interface ICurrentUserService
{
    string? UserId { get; }
    Caller? Caller { get; }
}
