// DISABLED for now: physical removal of a game (stream, tags and projected documents).
// Marten's DeleteSingleEventStreamAsync does not clean the DCB tag tables (mt_event_tag_*), so the foreign
// keys block it (error 23503). Needs a decision on how to remove tags before enabling.
// Related disabled pieces: HardDeleteGameConsumer, HardDeleteGameMessage, the registrations in Program.cs
// and the second half of CleanUpGamesConsumer.
//
// using Marten;
// using MediatR;
// using Splendor.Application.ReadModels;
// using Splendor.Application.Snapshots;
//
// namespace Splendor.Application.Commands;
//
// public record HardDeleteGameCommand(Guid GameId) : IRequest;
//
// public class HardDeleteGameCommandHandler : IRequestHandler<HardDeleteGameCommand>
// {
//     private readonly IDocumentStore _store;
//
//     public HardDeleteGameCommandHandler(IDocumentStore store)
//     {
//         _store = store;
//     }
//
//     public async Task Handle(HardDeleteGameCommand command, CancellationToken cancellationToken)
//     {
//         await _store.Advanced.Clean.DeleteSingleEventStreamAsync(command.GameId, ct: cancellationToken);
//
//         await using var session = _store.LightweightSession();
//         session.HardDelete<GameSummaryView>(command.GameId);
//         session.HardDelete<SplendorBoardView>(command.GameId);
//         session.HardDelete<SplendorGameState>(command.GameId);
//         await session.SaveChangesAsync(cancellationToken);
//     }
// }
