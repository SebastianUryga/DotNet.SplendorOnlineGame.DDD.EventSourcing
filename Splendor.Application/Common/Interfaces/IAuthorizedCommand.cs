using Splendor.Domain.Rules;

namespace Splendor.Application.Common.Interfaces;

/// <summary>Platform command: carries the caller (identity and roles), not just an owner id.</summary>
public interface IAuthorizedCommand
{
    Caller Caller { get; init; }
}
