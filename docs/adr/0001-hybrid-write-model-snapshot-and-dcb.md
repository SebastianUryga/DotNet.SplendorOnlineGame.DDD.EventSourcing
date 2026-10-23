# ADR 0001: Hybrid write model: stream snapshot for game state, DCB for cross-cutting rules

- **Status:** Accepted (2026-09-29, refined 2026-09-30)
- **Context area:** command side (Marten, event sourcing)

## Context

The first command side was a classic aggregate (`Game`). To avoid loading a growing history for every command, we tried Marten's Dynamic Consistency Boundary (DCB) in a spike: every command builds only the decision state it needs, selected by tag queries over `GameTag(gameId)`.

What the spike and the follow-up experiments showed:

- The core turn actions (`TakeGems`, `BuyCard`, `ReserveCard`, nobles, turn order, end of game) are tightly coupled. Per-command DCB states ended up rebuilding nearly the same wide game state three times.
- Cost grows with history. A synthetic benchmark (`Splendor.LoadTests`, 30 samples, simplified state, inline snapshot) gave these medians:

| Strategy | 100 events | 1,000 events | 10,000 events |
|---|---:|---:|---:|
| DCB (realistic) | 5.75 ms | 12.21 ms | 82.05 ms |
| Single stream, live aggregation | 4.44 ms | 7.37 ms | 68.14 ms |
| Single stream, inline snapshot | 1.26 ms | 1.32 ms | 1.20 ms |

- Events of one game were being spread over several physical streams (`boundary.AppendMany` routes by tags), so a single-stream snapshot did not see the whole history. Appending explicitly to the game stream while keeping the tags fixed this.

## Decision

- Gameplay commands load one `SplendorGameState` (an **async snapshot** of the game's single stream) with `FetchForWriting` and append tagged events to that stream. Stream version gives optimistic concurrency.
- DCB stays for **narrow rules that cross games or need little history**: `JoinGameDecisionState` (active games per owner), `CreateGameDecisionState` (created-games limit), `TurnClockState` (turn deadline).
- Decisions live in `Decide(command, state)` and in pure rule classes (`SplendorRules`, `PlatformRules`), so they are unit-tested without Marten.

## Alternatives considered

- **Classic aggregate:** simple, but replays the full history; removed.
- **One DCB state per command:** matches DCB's idea best, but for gameplay the boundaries overlap almost completely, so it only duplicates code. Rejected for gameplay, kept where boundaries really are small.
- **Live aggregation without snapshot:** cost still grows with history (68 ms at 10,000 events in the benchmark).

## Consequences

- Command load time is roughly flat in history length; concurrency conflicts are covered by integration tests (stream version and DCB tag version).
- Snapshot state needs a JSON round-trip test, because get-only collections are not populated by default.
- The benchmark numbers come from a simplified state and an *inline* snapshot; the API uses an *async* snapshot, which was not benchmarked. Re-measure only if a performance target or a regression appears.
- Stored event and snapshot JSON must stay readable when C# names change (`[JsonPropertyName]`, `StoredJsonCompatibilityTests`).
