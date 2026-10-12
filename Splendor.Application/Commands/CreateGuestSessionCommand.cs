using Marten;
using MediatR;

namespace Splendor.Application.Commands;

public class GuestSession
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>Result is null when MaxActive guests are already online.</summary>
public record CreateGuestSessionCommand(int MaxActive, TimeSpan Lifetime) : IRequest<GuestSession?>;

public class CreateGuestSessionCommandHandler : IRequestHandler<CreateGuestSessionCommand, GuestSession?>
{
    private readonly IDocumentSession _session;

    public CreateGuestSessionCommandHandler(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<GuestSession?> Handle(CreateGuestSessionCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // ponytail: count-then-insert can overshoot the limit by a request or two under a race; use a DB constraint if it matters
        var active = await _session.Query<GuestSession>().CountAsync(g => g.ExpiresAt > now, cancellationToken);
        if (active >= command.MaxActive)
            return null;

        var guest = new GuestSession { Id = $"guest-{Guid.NewGuid():N}", ExpiresAt = now.Add(command.Lifetime) };
        _session.Store(guest);
        await _session.SaveChangesAsync(cancellationToken);
        return guest;
    }
}
