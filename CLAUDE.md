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
│                  Splendor.BotWorker                          │
│  MassTransit consumer, API-driven bot client                 │
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
- **Bot worker**: MassTransit, strategie ruchów i Serilog (konsola + pliki)
- **Testowanie**: xUnit, Testcontainers, FluentAssertions, Selenium WebDriver

### Bot worker

`Splendor.BotWorker` jest zewnętrznym, API-driven klientem gry. Uwierzytelnia się jako zwykły użytkownik i wykonuje wszystkie akcje przez REST API; nie odwołuje się bezpośrednio do bazy danych, event store ani warstwy aplikacyjnej.

Worker konsumuje `GameUpdatedMessage` z RabbitMQ. `IBotGameMembershipHandler` obsługuje zaproszenie i dołączenie bota, a `IBotStrategy` wybiera ruch dla aktualnego stanu gry. Logi Serilog trafiają na konsolę oraz do `Splendor.BotWorker/logs/bot-worker-*.log`.

## Kluczowe pliki

### Domain Layer
| Plik | Opis |
|------|------|
| `Domain/Aggregates/Game.cs` | Główny agregat gry: odtwarza stan przez `Apply()`, obsługuje tworzenie i rozpoczęcie gry, tury, pobieranie i zwrot żetonów, zakup oraz rezerwację kart, wybór arystokraty i zakończenie gry. |
| `Domain/Events/GameEvents.cs` | Zdarzenia domenowe: utworzenie i rozpoczęcie gry, tury, żetony, przekroczenie i rozwiązanie limitu żetonów, zakup/rezerwacja/odsłonięcie kart, arystokraci, zakończenie i usunięcie gry. |
| `Domain/Entities/Player.cs` | Gracz: identyfikatory, nazwa, żetony, kupione i zarezerwowane karty oraz zdobyci arystokraci. |
| `Domain/ValueObjects/GemCollection.cs` | Kolekcja żetonów: Diamond, Sapphire, Emerald, Ruby, Onyx i Gold. |
| `Domain/ValueObjects/Card.cs` | Karta rozwoju: identyfikator, poziom, bonus, punkty prestiżu i koszt. |
| `Domain/ValueObjects/Noble.cs` | Arystokrata: identyfikator, punkty prestiżu i wymagane bonusy kart. |
| `Domain/ValueObjects/GemType.cs` | Enum kolorów żetonów i bonusów kart. |
| `Domain/CardDefinitions.cs` | Statyczne definicje 90 kart: 40 poziomu 1, 30 poziomu 2 i 20 poziomu 3. |
| `Domain/NobleDefinitions.cs` | Statyczne definicje arystokratów dostępnych w grze. |	

### Application Layer
| Plik | Opis |
|------|------|
| `Application/Commands/CreateGameCommand.cs` | Tworzenie nowej gry |
| `Application/Commands/JoinGameCommand.cs` | Dołączanie gracza do gry |
| `Application/Commands/StartGameCommand.cs` | Rozpoczęcie gry |
| `Application/Commands/TakeGemsCommand.cs` | Pobieranie gemów z rynku |
| `Application/Commands/BuyCardCommand.cs` | Kupowanie karty |
| `Application/Commands/DeleteGameCommand.cs` | Usuwanie gry |
| `Application/Commands/ResolveGemLimitCommand.cs` | Rozwiązywanie limitu gemów |
| `Application/Commands/ReserveCardCommand.cs` | Rezerwacja karty |
| `Application/Commands/ChooseNobleCommand.cs` | Wybór arystokraty |
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
|---|---|---|
| GET | `/games` | Lista gier |
| POST | `/games` | Tworzenie gry |
| GET | `/games/{id}` | Aktualny stan gry |
| GET | `/games/{id}/history` | Historia zdarzeń gry |
| DELETE | `/games/{id}` | Usuwanie gry |
| POST | `/games/{id}/players` | Dołączanie gracza |
| POST | `/games/{id}/invite` | Zapraszanie użytkownika do gry |
| POST | `/games/{id}/start` | Rozpoczęcie gry |
| POST | `/games/{id}/actions/take-gems` | Pobieranie żetonów |
| POST | `/games/{id}/actions/resolve-gem-limit` | Zwrot nadmiaru żetonów |
| POST | `/games/{id}/actions/buy-card` | Kupowanie karty |
| POST | `/games/{id}/actions/reserve-card` | Rezerwacja karty |
| POST | `/games/{id}/actions/choose-noble` | Wybór arystokraty |
| GET | `/games/{id}/available-actions` | Akcje dostępne dla aktywnego gracza |
| GET | `/games/{id}/version` | Wersja gry |
| GET | `/cards` | Definicje kart |
| GET | `/nobles` | Definicje arystokratów |

### Testy

```bash
dotnet test Splendor.UnitTests
dotnet test Splendor.IntegrationTests
dotnet test Splendor.UITests

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

## Aktualny stan

### Zaimplementowane

- Event Sourcing z Marten oraz CQRS z MediatR.
- Agregat `Game` z odtwarzaniem stanu przez `Apply(Event)`.
- Pełna talia Splendor: 90 kart rozwoju na trzech poziomach.
- Rozgrywka dla 2-4 graczy: tworzenie gry, lobby, dołączanie graczy i tury.
- Pobieranie żetonów zgodnie z regułami gry:
  - trzy różne kolory;
  - dwa żetony tego samego koloru, gdy w rynku są co najmniej cztery;
  - limit 10 żetonów z obowiązkowym zwrotem nadmiaru.
- Kupowanie kart z uwzględnieniem stałych bonusów i złotych żetonów jako wildcardów.
- Rezerwacja kart: maksymalnie trzy na gracza; rezerwacja przyznaje złoty żeton, jeśli jest dostępny.
- Arystokraci: automatyczne przyznanie jednego dostępnego arystokraty albo wybór, gdy gracz kwalifikuje się do kilku.
- Zakończenie gry po osiągnięciu co najmniej 15 punktów prestiżu.
- Read model `GameView`, projekcje Marten i modele odczytu EF Core w SQL Server.
- REST API, Swagger, JWT/Auth0, SignalR oraz MassTransit/RabbitMQ.
- ETag/`304 Not Modified` dla `GET /games/{id}` i polling wersji gry.
- Testy jednostkowe agregatu w `Splendor.UnitTests`.
- Testy integracyjne z Testcontainers oraz testy UI Selenium z Page Object Model.


### Do zrobienia

**Frontend:**
- [ ] Nazywanie gry (przy tworzeniu)
- [ ] Wyświetlanie nazw graczy w games-list
- [ ] Total gems dla gracza w gameplay view

**Bot worker:**
- [ ] Zapewnić odczyt `GameView` co najmniej w wersji wskazanej przez `GameUpdatedMessage`; przy opóźnionej projekcji stosować ograniczone retry/redelivery zamiast wykonywać ruch na starym stanie.
- [ ] Rozważyć `IBotGameRegistry` jako cache gier i playerów kontrolowanych przez bota; cache nie może być źródłem prawdy i musi umieć odbudować stan po restarcie workera.

## Konwencje kodu

- Eventy jako `record` w `GameEvents.cs`
- Agregat stosuje eventy przez metody `Apply(Event)`
- Metody domenowe zwracają `IEnumerable<IDomainEvent>`
- Komendy obsługiwane przez MediatR handlery
- Projekcje Marten aktualizują EF read models
- Elementy UI testowalne oznaczane atrybutem `data-testid` w szablonach Angular
