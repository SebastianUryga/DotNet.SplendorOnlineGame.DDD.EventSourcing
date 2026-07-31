# CLAUDE.md - Kontekst projektu Splendor

## Preferencje użytkownika
- **Oszczędzanie tokenów**: Przed edycją plików najpierw pokaż planowane zmiany w całości i zapytaj o akceptację. Nie rób wielu małych edycji - grupuj zmiany.

## Opis projektu
Implementacja gry planszowej **Splendor** jako część platformy SplendorOnlineGame.
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
- **Testowanie**: xUnit, Testcontainers, FluentAssertions, Selenium WebDriver

## Kluczowe pliki

### Domain Layer
| Plik | Opis |
|------|------|
| `Domain/Aggregates/Game.cs` | Główny agregat gry - Apply() dla eventów, metody komend (JoinGame, StartGame, TakeGems, BuyCard) |
| `Domain/Events/GameEvents.cs` | Wszystkie eventy: GameCreated, PlayerJoined, GameStarted, TurnStarted, GemsTaken, TurnEnded, CardPurchased, CardRevealed, GameFinished, GameDeleted |
| `Domain/Entities/Player.cs` | Encja gracza (Id, OwnerId, Name, Gems, OwnedCardIds) |
| `Domain/ValueObjects/GemCollection.cs` | Value Object dla kolekcji gemów (Diamond, Sapphire, Emerald, Ruby, Onyx, Gold) |
| `Domain/ValueObjects/Card.cs` | Value Object karty (Id, Level, BonusType, PrestigePoints, Cost) |
| `Domain/ValueObjects/GemType.cs` | Enum typów gemów |
| `Domain/CardDefinitions.cs` | Statyczna definicja kart (90 kart: 40x L1, 30x L2, 20x L3) |

### Application Layer
| Plik | Opis |
|------|------|
| `Application/Commands/CreateGameCommand.cs` | Tworzenie nowej gry |
| `Application/Commands/JoinGameCommand.cs` | Dołączanie gracza do gry |
| `Application/Commands/StartGameCommand.cs` | Rozpoczęcie gry |
| `Application/Commands/TakeGemsCommand.cs` | Pobieranie gemów z rynku |
| `Application/Commands/BuyCardCommand.cs` | Kupowanie karty |
| `Application/Commands/DeleteGameCommand.cs` | Usuwanie gry |
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

### Integration Tests
| Plik | Opis |
|------|------|
| `IntegrationTests/SplendorApiFactory.cs` | WebApplicationFactory - konfiguracja testów z Testcontainers |
| `IntegrationTests/BasicTests.cs` | Podstawowe testy API (Swagger, Create Game) |
| `IntegrationTests/TestAuthHandler.cs` | Fake authentication handler - czyta X-Test-User-Id header |
| `IntegrationTests/TestUserContext.cs` | DelegatingHandler ustawiający X-Test-User-Id per request |

#### Konfiguracja testów integracyjnych
- **PostgreSQL** (Testcontainers) - dla Marten Event Store
- **SQL Server** (Testcontainers) - dla EF Core Read Models
- **Auth bypass** - `TestAuthHandler` omija JWT, user ID z headera `X-Test-User-Id`
- **MassTransit InMemory** - bez RabbitMQ, bus działa w pamięci

### UI Tests (Splendor.UITests)
| Plik | Opis |
|------|------|
| `UITests/Infrastructure/TestBase.cs` | Base class - tworzy ChromeDriver, ustawia token przez UI |
| `UITests/Infrastructure/TestSettings.cs` | Centralna konfiguracja (BaseUrl, TestToken) |
| `UITests/Infrastructure/DriverFactory.cs` | Fabryka ChromeDriver z auto-dopasowaniem wersji |
| `UITests/Pages/GamesListPage.cs` | POM dla listy gier - `ClickGame(gameId)` szuka kafelka po prefiksie ID |
| `UITests/Pages/LobbyPage.cs` | POM dla lobby - `GetGameId()` wyciąga ID z URL, `NavigateTo(gameId)` |
| `UITests/Pages/GamePage.cs` | POM dla widoku rozgrywki |
| `UITests/Tests/GameFlowTests.cs` | Pojedynczy E2E test pokrywający pełny flow gry (lista → lobby → dołączenie 2 graczy → start → rozgrywka) |

#### Konfiguracja UI testów
- Wymagają uruchomionego API (profil **Testing** w VS) i `ng serve`
- API w trybie Testing używa `TestAuthHandler` (plik `Api/Testing/TestAuthHandler.cs`)
- API w trybie Testing używa MassTransit InMemory (SignalR działa bez RabbitMQ)
- Selektory przez `data-testid` - niezależne od klas CSS i treści
- `WebDriverManager` auto-pobiera ChromeDriver pasujący do zainstalowanego Chrome
- Token ustawiany przez istniejący input w nagłówku aplikacji (`app.component.ts`)

do poprawienia: przez jakis madrzejszy LNM.:
----
### Testy jednostkowe i nowe zdarzenia zwrotu gemów (WIP)

- Nowy projekt: `Splendor.UnitTests`
  - Cel: szybkie testy jednostkowe, które rekonstruują agregat z listy eventów, wywołują metody-komend agregatu i asercują zwrócone eventy.
  - Dostępne helpery: `TestHelpers.CreateStartedGame()`, `TestHelpers.ApplyHistory()` (używane do przygotowania stanu gry). Uruchamianie: `dotnet test ./Splendor.UnitTests`

- Nowe zdarzenia domenowe (wprowadzono, wiring w toku):
  - `GemsOverflowDetected` (GameId, PlayerId, CurrentGems, ExcessCount, Timestamp) — emisja gdy `TakeGems` spowoduje, że suma gemów gracza przekroczy limit (10). Agregat przechodzi w stan oczekiwania na zwrot.
  - `GemLimitResolved` (GameId, PlayerId, ReturnedGems, Timestamp) — emisja gdy gracz zwróci gemy i limit zostanie spełniony. Po tym emitowane jest zakończenie tury / rozpoczęcie następnej.

- Zadania integracyjne / TODO (wymagają ręcznego dokończenia):
  - Application: zaktualizować `ResolveGemLimitCommand` aby wywoływał `Game.ResolveGemLimit(...)` i zapisywał wszystkie eventy zwrócone przez agregat (w tym TurnEnded/TurnStarted).
  - DI: zarejestrować nowy handler i pipeline MediatR w `DependencyInjection.cs`.
  - Projekcje / DB: zaktualizować projekcje Marten i projektory read-modeli, aby obsługiwały `GemsOverflowDetected` i `GemLimitResolved` (oznaczać read-model jako oczekujący zwrot, stosownie aktualizować MarketGems i usuwać flagę oczekiwania).
  - API / Controller: dodać obsługę endpointu akceptującego Resolve/Return (lub rozszerzyć istniejący przepływ TakeGems), zwracać walidacje/pending state do klienta.
  - UI: wyświetlić modal po wykryciu overflow, pozwolić użytkownikowi wybrać gemy do zwrotu i wywołać ResolveGemLimit (lub skonsolidowany endpoint), obsłużyć kontynuację tury.
  - Tests: dodać testy jednostkowe i integracyjne dla ResolveGemLimit oraz przypadków brzegowych (niepoprawny zwrot, konkurencja wersji itp.).

> Uwaga: zmiany są w trakcie pracy — eventy i testy jednostkowe zostały dodane, ale pełne powiązanie z warstwą aplikacji, projekcjami i UI nie jest jeszcze ukończone.
----
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
   - `GemsTaken` - pobranie gemów (max 3 różne, 2 takie same przy rynku >= 4 lub 1 złoty)
   - `CardPurchased` + `CardRevealed` - kupno karty
6. Gdy gracz osiągnie ≥ 15 punktów prestiżu → `GameFinished` (koniec gry, status `Finished`)
7. W przeciwnym wypadku `TurnEnded` - koniec tury i powrót do punktu 4 (następny gracz)

## API Endpoints

| Metoda | Endpoint | Opis |
|--------|----------|------|
| GET | `/games?includeDeleted=false` | Lista gier (z opcjonalnym filtrem `includeDeleted`) |
| POST | `/games` | Tworzenie nowej gry |
| GET | `/games/{id}` | Stan gry (GameView) |
| DELETE | `/games/{id}` | Usuwanie gry (zmiana statusu na `Deleted`) |
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

# Uruchamianie w trybie testowym (TestAuthHandler, MassTransit InMemory)
dotnet run --project Splendor.Api --launch-profile Testing

# Testy integracyjne
dotnet test Splendor.IntegrationTests

# Testy UI Selenium (wymaga: API w trybie Testing + ng serve)
dotnet test Splendor.UITests

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
- [x] Rezerwacja kart (ReserveCard command, CardReserved event, API endpoint, read-model + EF mapping, frontend UI, unit test)
- [x] Autentykacja JWT (Auth0) + ICurrentUserService
- [x] Middleware obsługi wyjątków (ExceptionHandlingMiddleware)
- [x] Endpoint GET /cards (definicje kart z backendu)
- [x] Endpoint GET /games (lista gier)
- [x] Frontend Angular (lista gier, lobby, widok rozgrywki)
- [x] Polling wersji gry (auto-refresh)
- [x] Walidacja reguł gemów w UI (3 różne lub 2 takie same przy >=4)
- [x] ETag/304 dla GET /games/{id} (cache po stronie klienta)
- [x] Testy UI Selenium z Page Object Model (Splendor.UITests)

### Do zrobienia

**Backend:**
- [ ] Pełna walidacja reguł pobierania gemów
- [ ] Noble tiles (arystokraci)
- [ ] Warunek zakończenia gry (15 punktów)
- [ ] Pełna lista kart (90 zamiast MVP subset)

**Frontend:**
- [ ] Nazywanie gry (przy tworzeniu)
- [ ] Wyświetlanie nazw graczy w games-list
- [ ] Total gems dla gracza w gameplay view
- [ ] Wyświetlanie zakupionych kart wg koloru (analogicznie do żetonów, z nagłówkiem "Cards")

## Konwencje kodu

- Eventy jako `record` w `GameEvents.cs`
- Agregat stosuje eventy przez metody `Apply(Event)`
- Metody domenowe zwracają `IEnumerable<IDomainEvent>`
- Komendy obsługiwane przez MediatR handlery
- Projekcje Marten aktualizują EF read models
- Elementy UI testowalne oznaczane atrybutem `data-testid` w szablonach Angular
