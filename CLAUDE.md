# CLAUDE.md - Kontekst projektu Splendor

## Opis projektu
Implementacja gry planszowej **Splendor** jako część platformy EventSource.BoardGamesArena.
Backend w .NET z wykorzystaniem **Event Sourcing**, **CQRS** i **DDD**.

## Architektura

```
┌─────────────────────────────────────────────────────────────┐
│                     Splendor.Web                            │
│  Angular 16 (standalone), Port: 4200                        │
├─────────────────────────────────────────────────────────────┤
│                     Splendor.Api                            │
│  Controllers, Middleware, Program.cs                        │
│  Port: 5081, Swagger: /swagger                              │
├─────────────────────────────────────────────────────────────┤
│                  Splendor.Application                       │
│  Commands (MediatR), Queries, ReadModels                    │
├─────────────────────────────────────────────────────────────┤
│                    Splendor.Domain                          │
│  Aggregates, Entities, Events, ValueObjects                 │
├─────────────────────────────────────────────────────────────┤
│                 Splendor.Infrastructure                     │
│  Persistence (Marten/EF), Projections, Migrations           │
└─────────────────────────────────────────────────────────────┘
```

### Tech Stack
- **Frontend**: Angular 16 (standalone components), czysty CSS
- **Event Store**: Marten (PostgreSQL) - zapis eventów
- **Read Models**: EF Core (SQL Server) - projekcje do odczytu
- **CQRS**: MediatR - obsługa komend i zapytań
- **Testowanie**: xUnit, Testcontainers, FluentAssertions

## Kluczowe pliki

### Domain Layer
| Plik | Opis |
|------|------|
| `Domain/Aggregates/Game.cs` | Główny agregat gry - Apply() dla eventów, metody komend (JoinGame, StartGame, TakeGems, BuyCard) |
| `Domain/Events/GameEvents.cs` | Wszystkie eventy: GameCreated, PlayerJoined, GameStarted, TurnStarted, GemsTaken, TurnEnded, CardPurchased, CardRevealed |
| `Domain/Entities/Player.cs` | Encja gracza (Id, OwnerId, Name, Gems, OwnedCardIds) |
| `Domain/ValueObjects/GemCollection.cs` | Value Object dla kolekcji gemów (Diamond, Sapphire, Emerald, Ruby, Onyx, Gold) |
| `Domain/ValueObjects/Card.cs` | Value Object karty (Id, Level, BonusType, PrestigePoints, Cost) |
| `Domain/ValueObjects/GemType.cs` | Enum typów gemów |
| `Domain/CardDefinitions.cs` | Statyczna definicja kart (MVP: subset, pełna gra ma 90 kart) |

### Application Layer
| Plik | Opis |
|------|------|
| `Application/Commands/CreateGameCommand.cs` | Tworzenie nowej gry |
| `Application/Commands/JoinGameCommand.cs` | Dołączanie gracza do gry |
| `Application/Commands/StartGameCommand.cs` | Rozpoczęcie gry |
| `Application/Commands/TakeGemsCommand.cs` | Pobieranie gemów z rynku |
| `Application/Commands/BuyCardCommand.cs` | Kupowanie karty |
| `Application/ReadModels/GameView.cs` | Read model gry (GameView, PlayerView) |

### Infrastructure Layer
| Plik | Opis |
|------|------|
| `Infrastructure/Projections/GameProjection.cs` | Marten projection - aktualizuje GameView na podstawie eventów |
| `Infrastructure/Persistence/ReadModelsContext.cs` | EF Core DbContext dla read models |
| `Infrastructure/DependencyInjection.cs` | Rejestracja serwisów Marten i EF |

### API Layer
| Plik | Opis |
|------|------|
| `Api/Controllers/GamesController.cs` | REST API dla gier i kart |
| `Api/Program.cs` | Konfiguracja aplikacji (CORS, JWT, etc.) |
| `Api/Middleware/` | Custom middleware (ExceptionHandling) |

### Frontend (Splendor.Web)
| Plik | Opis |
|------|------|
| `src/app/core/services/auth.service.ts` | Zarządzanie JWT tokenem (localStorage) |
| `src/app/core/services/game.service.ts` | Komunikacja z API + cache kart |
| `src/app/core/interceptors/auth.interceptor.ts` | Dodaje Bearer token do requestów |
| `src/app/pages/games-list/` | Lista gier + tworzenie nowej |
| `src/app/pages/lobby/` | Lobby gry (dołączanie, start) |
| `src/app/pages/game/` | Widok rozgrywki (rynek gemów, karty, gracze) |
| `src/app/models/` | Interfejsy TS (GameView, GemCollection, Card, requests) |
| `src/environments/environment.ts` | Konfiguracja API URL |

## Model danych

### Identyfikatory (ważne!)
- **GameId**: `Guid` - identyfikator gry
- **OwnerId**: `string` - identyfikator użytkownika (np. z JWT sub claim)
- **PlayerId**: `string` - wewnętrzny identyfikator gracza w grze (generowany jako `Guid + " " + Name`)

Jeden OwnerId może mieć wielu Players w różnych grach. PlayerId jest unikalny w ramach gry.

### GemCollection
```csharp
record GemCollection(int Diamond, int Sapphire, int Emerald, int Ruby, int Onyx, int Gold)
```
- Wspiera operatory `+` i `-`
- `Gold` = żeton złoty (wildcard)
- Startowy rynek: `(4, 4, 4, 4, 4, 5)` - 4 każdego koloru + 5 złotych

### Karty
- 3 poziomy (Level 1, 2, 3)
- Każda karta daje: bonus typu gemu + punkty prestiżu
- Rynek: 4 karty widoczne na poziom
- Talie: pozostałe karty do dobrania

## Przepływ gry (eventy)

1. `GameCreated` - utworzenie gry przez CreatorId
2. `PlayerJoined` - dołączenie gracza (2-4 graczy)
3. `GameStarted` - rozpoczęcie (tasowanie talii, setup rynku)
4. `TurnStarted` - początek tury gracza
5. Akcja gracza:
   - `GemsTaken` - pobranie gemów (max 3 różne lub 2 takie same)
   - `CardPurchased` + `CardRevealed` - kupno karty
6. `TurnEnded` - koniec tury
7. Powrót do punktu 4 (następny gracz)

## API Endpoints

| Metoda | Endpoint | Opis |
|--------|----------|------|
| GET | `/games` | Lista wszystkich gier |
| POST | `/games` | Tworzenie nowej gry |
| GET | `/games/{id}` | Stan gry (GameView) |
| POST | `/games/{id}/players` | Dołączanie gracza |
| POST | `/games/{id}/start` | Start gry |
| POST | `/games/{id}/actions/take-gems` | Pobieranie gemów |
| POST | `/games/{id}/actions/buy-card` | Kupowanie karty |
| GET | `/games/{id}/version` | Wersja gry (do pollingu) |
| GET | `/cards` | Definicje wszystkich kart |

## Komendy

### Backend
```bash
# Budowanie
dotnet build

# Uruchamianie (wymaga Docker)
docker-compose up -d
dotnet run --project Splendor.Api

# Testy
dotnet test

# Migracje EF
cd Splendor.Infrastructure
dotnet ef migrations add <NazwaMigracji> --startup-project ../Splendor.Api
```

### Frontend
```bash
cd Splendor.Web

# Instalacja zależności
npm install

# Uruchamianie (dev)
ng serve
# lub
npm start

# Aplikacja dostępna na http://localhost:4200
```

## Aktualny stan (co jest zaimplementowane)

### Zrobione
- [x] Event Sourcing z Marten
- [x] CQRS z MediatR
- [x] Agregat Game z podstawowymi eventami
- [x] Komendy: CreateGame, JoinGame, StartGame, TakeGems, BuyCard
- [x] Read model GameView z projekcją
- [x] REST API z Swagger + CORS
- [x] Testy integracyjne z Testcontainers
- [x] Rozdzielenie OwnerId (użytkownik) od PlayerId (gracz w grze)
- [x] System kart (definicje, rynek, talie)
- [x] Kupowanie kart z bonusami
- [x] Autentykacja JWT (Auth0) + ICurrentUserService
- [x] Middleware obsługi wyjątków (ExceptionHandlingMiddleware)
- [x] Endpoint GET /cards (definicje kart z backendu)
- [x] Endpoint GET /games (lista gier)
- [x] Frontend Angular (lista gier, lobby, widok rozgrywki)
- [x] Polling wersji gry (auto-refresh)
- [x] Walidacja reguł gemów w UI (3 różne lub 2 takie same przy >=4)

### Do zrobienia
- [ ] Pełna walidacja reguł pobierania gemów (backend)
- [ ] Rezerwacja kart
- [ ] Noble tiles (arystokraci)
- [ ] Warunek zakończenia gry (15 punktów)
- [ ] Pełna lista kart (90 zamiast MVP subset)

## Konwencje kodu

- Eventy jako `record` w `GameEvents.cs`
- Agregat stosuje eventy przez metody `Apply(Event)`
- Metody domenowe zwracają `IEnumerable<IDomainEvent>`
- Komendy obsługiwane przez MediatR handlery
- Projekcje Marten aktualizują EF read models
