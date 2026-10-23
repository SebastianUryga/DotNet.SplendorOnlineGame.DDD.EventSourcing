# ADR 0002: The bot is an external client of the public API

- **Status:** Accepted (2026-10-22 for the batching and delay details)
- **Context area:** bot worker, messaging

## Context

The game needs automated opponents. A bot can live inside the application (calling command handlers directly) or outside it, as another client.

## Decision

`Splendor.BotWorker` is a separate process that signs in as an ordinary user (role `bot`) and plays only through the REST API. It listens to `GameUpdatedMessage` from RabbitMQ and picks moves with an `IBotStrategy`.

How it behaves:

- **Batches, latest message per game.** One move produces several events, hence several messages. The consumer batches them and handles the newest per game, which prevents an avalanche of repeated work.
- **One batch at a time** (`ConcurrencyLimit = 1`), games inside a batch run in parallel. A game appears once per batch, so it is never processed twice at the same time, and no per-game lock is needed.
- **Move delay** (`Bot:MoveDelayMs`, default 2000 ms), applied only before a move and only after checking that it is the bot's turn. It lets client animations finish. Joining a game is not delayed.
- **Joining.** The bot joins when invited. If the inviter is not a player in that game (the creator is only watching), the bot takes two seats, which gives a "watch bot vs bot" game owned by the guest.
- The bot is subject to the same platform rules as anyone, with higher limits for its role (40 active, 20 created games).

## Alternatives considered

- **In-process bot calling handlers:** shorter path, but it bypasses authorization, validation and limits, and couples the bot to the application layer.
- **Plain consumer partitioned by game id:** no batching means every event triggers a move check, and moves multiply messages.
- **Delaying with a message scheduler:** CloudAMQP Free has no delayed-message plugin, and the in-memory scheduler loses timers on restart.

## Consequences

- The bot exercises the same rules and the public API as a human client, and can be deployed separately (today it shares the Render container with the API).
- A move takes about 3 s: 2 s of deliberate delay plus roughly 1 s of event pipeline latency (seen in local logs; the cause is not verified).
- The bot is a single account, so it shares the API rate limits (`GameActions` 10 per 5 s, `Global` 60 per 10 s). Many simultaneous bot games may hit them; not yet measured in production, and the bot has no metrics.
- Event filtering for the bot (only invitations and turn starts) is a possible later optimisation. The waiting phases (gem return, noble choice) must stay included.
