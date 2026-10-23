# CLAUDE.md - Kontekst projektu Splendor

## Preferencje użytkownika
- **Oszczędzanie tokenów**: Przed edycją plików najpierw pokaż planowane zmiany w całości i zapytaj o akceptację. Nie rób wielu małych edycji - grupuj zmiany.

## Wartości projektu
- Generyczność i rozszerzalność: nowe reguły, dodatki i kolejne gry przez dodawanie kodu, nie modyfikację istniejącego. Stan obecny nie jest docelowy, więc oceniaj rozwiązania konkretnymi scenariuszami zmian, a nie tylko dzisiejszym Splendorem. Nie uogólniaj z jednego przykładu.
- Szczegóły i otwarte pytania: notatka Obsidian "Wartości i założenia architektoniczne".

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
│  Persistence (Marten), Projections                          │
└─────────────────────────────────────────────────────────────┘
```

### Tech Stack
- **Frontend**: Angular 16 (standalone components), czysty CSS
- **Event Store**: Marten (PostgreSQL) - zapis eventów
- **Read Models**: dokumenty Marten (PostgreSQL) - projekcje do odczytu
- **CQRS**: MediatR - obsługa komend i zapytań
- **Bot worker**: MassTransit, strategie ruchów i Serilog (konsola + pliki)
- **Testowanie**: xUnit, Testcontainers, FluentAssertions, Selenium WebDriver

### Bot worker

`Splendor.BotWorker` jest zewnętrznym, API-driven klientem gry. Uwierzytelnia się jako zwykły użytkownik i wykonuje wszystkie akcje przez REST API; nie odwołuje się bezpośrednio do bazy danych, event store ani warstwy aplikacyjnej.

Worker konsumuje `GameUpdatedMessage` z RabbitMQ. `StreamVersion` wiadomości służy wyłącznie do wybrania najnowszego eventu w batchu i nie jest porównywana z wersją read modelu. `IBotGameMembershipHandler` obsługuje zaproszenie i dołączenie bota, a `IBotStrategy` wybiera ruch dla aktualnego stanu gry. `GameUpdatedMessage.Data` niesie zdarzenie jako JSON; przy `PlayerInvited` bot dołącza tylko, gdy `InviteeId` to jego identyfikator. Na produkcji bot działa w tym samym kontenerze co API (Api__BaseUrl=http://localhost:10000, konto bota w Auth0 przez `Bot__Username`/`Bot__Password`). Logi Serilog trafiają na konsolę oraz do `Splendor.BotWorker/logs/bot-worker-*.log`.

Przed ruchem bot czeka `Bot:MoveDelayMs` (domyślnie 2000, `0` wyłącza; `BotOptions`), żeby animacje na froncie nadążały; sprawdzenie tury jest przed czekaniem, a dołączanie do gry nie czeka. Konsument batcha (`ConcurrencyLimit = 1`) przetwarza gry z batcha równolegle (`Task.WhenAll`) i nigdy tej samej gry dwa razy naraz. Gdy bot zostanie zaproszony przez kogoś, kto nie jest graczem w tej grze (twórca-widz), zajmuje dwa miejsca (`Bot-A`, `Bot-B`): tak powstaje mecz bot vs bot.

## Kluczowe pliki

### Domain Layer
| Plik | Opis |
|------|------|
| `Domain/Rules/SplendorRules.cs` | Czyste reguły domenowe: bonusy, punkty prestiżu, wymagania arystokratów, walidacja żetonów, płatność za karty, limit rezerwacji, wybór zwycięzcy, limit żetonów (`MaxPlayerGems`), próg końca gry (`EndGamePrestigePoints`) i automatyczny wybór żetonów do zwrotu (`ChooseGemsToReturn`). |
| `Domain/Rules/PlatformRules.cs` | Reguły platformy (nie gry): kto usuwa grę (twórca lub admin), limity aktywnych (2, dla roli `bot` 40) i utworzonych (2, dla `bot` 20) gier, łączny limit utworzonych gier dla gościa (10, `WithinTotalCreatedLimit`; liczy też usunięte i zakończone), wiek nieaktywnej gry, granice czasu tury (domyślnie 60 s, 30-300 s). |
| `Domain/Rules/Caller.cs` | Kto wywołuje komendę: `UserId` (value object) i role (`admin`, `guest`, `bot`); `Caller.System` dla zadań w tle. |
| `Domain/Events/GameEvents.cs` | Zdarzenia domenowe: utworzenie i rozpoczęcie gry, tury, żetony, przekroczenie i rozwiązanie limitu żetonów, zakup/rezerwacja/odsłonięcie kart, arystokraci, zakończenie i usunięcie gry. `GameCreated` niesie opcjonalne `Name` i `TurnDurationSeconds` (domyślnie 60). |
| `Domain/Entities/Player.cs` | Gracz: identyfikatory, nazwa, żetony, kupione i zarezerwowane karty oraz zdobyci arystokraci. |
| `Domain/ValueObjects/GemCollection.cs` | Kolekcja żetonów: Diamond, Sapphire, Emerald, Ruby, Onyx i Gold. |
| `Domain/ValueObjects/GameName.cs` | Nazwa gry: 1-40 znaków po trim; tworzona w kontrolerze. |
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
| `Application/Commands/InvitePlayerCommand.cs` | Zapraszanie gracza do lobby (twórca gry lub uczestnik) |
| `Application/Commands/LeaveGameCommand.cs` | Opuszczanie lobby przez gracza |
| `Application/Commands/StartGameCommand.cs` | Rozpoczęcie gry |
| `Application/Commands/TakeGemsCommand.cs` | Pobieranie gemów z rynku |
| `Application/Commands/BuyCardCommand.cs` | Kupowanie karty |
| `Application/Commands/DeleteGameCommand.cs` | Usuwanie gry |
| `Application/Commands/ResolveGemLimitCommand.cs` | Rozwiązywanie limitu gemów |
| `Application/Commands/ReserveCardCommand.cs` | Rezerwacja karty |
| `Application/Commands/ChooseNobleCommand.cs` | Wybór arystokraty |
| `Application/Commands/ExpireTurnCommand.cs` | Automatyczne zakończenie przeterminowanej tury; oczekujący zwrot żetonów lub wybór arystokraty jest rozstrzygany automatycznie |
| `Application/Commands/CreateGuestSessionCommand.cs` | Sesja gościa: pilnuje limitu aktywnych gości; zwraca `null`, gdy limit osiągnięty (kontroler odpowiada 429). |
| `Application/Commands/TurnCompletion.cs` | Koniec akcji i tury: przepełnienie żetonów, arystokraci, koniec gry, następna tura i deadline. |
| `Application/DecisionStates/TurnClockState.cs` | Granica DCB zegara tury; niesie `DurationSeconds` z `GameCreated`. |
| `Application/ReadModels/GameView.cs` | Read model gry (GameView, PlayerView) |
| `Application/Behaviors/CommandMetricsBehavior.cs` | MediatR pipeline behavior mierzący czas, aktywne i zakończone operacje. |
| `Application/Behaviors/EventMetadataBehavior.cs` | Ustawia na sesji Martena `UserName` (z `Caller`, `system` dla komend ze schedulera) i `CorrelationId` zapisywane w metadanych eventów. |
| `Application/Events/EventTagger.cs` | Jedyne miejsce tagowania eventów (`GameTag`, `PlayerOwnerTag`, `GameCreatorTag`); `GameDeleted`/`GameFinished` dostają twórcę z `state.GameCreatorId`. |
| `Application/DecisionStates/CreateGameDecisionState.cs` | Granica DCB limitu utworzonych gier (po `GameCreatorTag`): `OpenGameIds` (otwarte) i `TotalCreated` (wszystkie kiedykolwiek, dla limitu gościa). |

### Infrastructure Layer
| Plik | Opis |
|------|------|
| `Infrastructure/DependencyInjection.cs` | Rejestracja Marten, projekcji i subskrypcji eventów |
| `Infrastructure/Projections/GameSummaryProjection.cs` | Marten projection dla listy gier |
| `Infrastructure/Projections/SplendorBoardProjection.cs` | Marten projection dla planszy Splendor |
| `Infrastructure/Observability/MartenMetricsLogger.cs` | Pomiar czasu i liczby operacji wykonywanych przez Martena. |

### API Layer
| Plik | Opis |
|------|------|
| `Api/Controllers/GamesController.cs` | REST API dla gier i kart |
| `Api/Controllers/AuthController.cs` | `POST /auth/guest`: wysyła `CreateGuestSessionCommand`, token HS256 wystawia `GuestTokens`. |
| `Api/Program.cs` | Konfiguracja aplikacji (CORS, JWT, etc.) |
| `Api/Middleware/` | Custom middleware (ExceptionHandling) |
| `Api/Services/GameCleanupTimer.cs` | `BackgroundService`: co godzinę publikuje `CleanUpGamesMessage` (przez singletonowy `IBus`); działa tylko gdy API nie śpi, nie jest rejestrowany w trybie Testing. |
| `Api/Consumers/CleanUpGamesConsumer.cs` | Sprzątanie gier: wybiera przez `GetGamesQuery` gry nieaktywne >48 h i publikuje po jednej `DeleteStaleGameMessage` na grę; scope, retry i kolejka `_error` per gra pochodzą z MassTransit. Fizyczne kasowanie (`HardDeleteGame`) jest wyłączone (zakomentowane). |
| `Api/Consumers/GameUpdatedConsumer.cs` | Wysyła `GameView` i listę `GameNotification(Type, PlayerId, Data)` do grupy SignalR. `Data` budowane ręcznie per typ (biała lista): `card_purchased` → `cardId`, `paidGems`; `gems_taken` → `gems`; `noble_acquired` → `nobleId`; `card_reserved` bez `cardId` (rezerwacja z talii ujawniłaby zakrytą kartę). |
| `Api/Consumers/DeleteStaleGameConsumer.cs` | Wysyła `DeleteGameCommand` jako `Caller.System`. `HardDeleteGameConsumer` i `HardDeleteGameCommand` są zakomentowane: `DeleteSingleEventStreamAsync` nie czyści tabel tagów `mt_event_tag_*`. |
| `Api/Auth/RoleClaims.cs` | Nazwa claima z rolami (`https://splendoronlinegame-web.onrender.com/roles`), dodawanego przez Auth0 Action oraz w tokenie gościa. |

### Obserwowalność

- API eksportuje metryki i trace’y przez OpenTelemetry Protocol do Aspire Dashboard. Na produkcji eksport idzie do Grafana Cloud przez standardowe zmienne `OTEL_EXPORTER_OTLP_ENDPOINT/PROTOCOL/HEADERS` (lokalnie `OpenTelemetry:Endpoint`); meter `System.Runtime` daje pamięć procesu API. Bot nie ma instrumentacji.
- MediatR mierzy `splendor.command.duration`, `splendor.command.executed` i `splendor.command.active`.
- Marten mierzy operacje odczytu i zapisu przez `MartenMetricsLogger`.
- Dashboard lokalny jest dostępny pod `http://localhost:18888` po uruchomieniu `docker-compose up -d`.
- Benchmark w `Splendor.LoadTests` porównał DCB, agregację live i snapshot inline na uproszczonym stanie i uzasadnił przejście na snapshoty. Pomiar pełnego `SplendorGameState` nie jest planowany bez konkretnego celu wydajnościowego lub wykrytej regresji.

### Wdrożenie i dostęp

- **Hosting (plany darmowe):** Render (API + bot w jednym kontenerze z `Dockerfile`, frontend jako Static Site), Neon (PostgreSQL), CloudAMQP (RabbitMQ), Auth0, CI w Azure DevOps (`azure-pipelines.yml`, agent lokalny). Render usypia API po 15 min bez ruchu.
- **Zmienne środowiskowe API:** `ConnectionStrings__Marten`, `RabbitMq__Url` (`amqps://...`, bez niej `localhost/guest`), `Guest__SigningKey` (wymagany poza trybem Testing), `AllowedOrigins__0`, `ASPNETCORE_ENVIRONMENT`; opcjonalnie `Guest__MaxActive`, `Guest__TokenMinutes`.
- **Tryb gościa:** `POST /auth/guest` wystawia token HS256 (issuer `splendor-guest`), `CreateGuestSessionCommand` zapisuje `GuestSession` w Marten i pilnuje limitu aktywnych gości. Schemat uwierzytelniania `Smart` wybiera Auth0 albo gościa po `iss`.
- **Rate limiting:** polityki z sekcji `RateLimits` (`GuestAuth` po IP, `GameActions` po użytkowniku), rejestrowane w `Api/Auth/RateLimitingExtensions.cs`; nowy limit to wpis w konfiguracji i `[EnableRateLimiting("Nazwa")]`. Wpis `Global` (domyślnie 60 żądań/10 s po użytkowniku, fallback na IP) to `GlobalLimiter` obejmujący każdy endpoint, także bez atrybutu. Za proxy Rendera działa `UseForwardedHeaders`.
- **Scheduler tur:** `AddMessageScheduler` + `UseInMemoryScheduler` (CloudAMQP Free nie ma delayed exchange), więc zaplanowane timery giną przy restarcie procesu.
- **Lokalnie:** hasła nie trafiają do `appsettings.json` w git.
- **Caller i role:** komendy implementują `IAuthorizedCommand` i niosą `Caller` (id + role z claima `RoleClaims.Type`) zamiast `OwnerId`; kontroler bierze go z `ICurrentUserService.Caller`. Rola `admin` pochodzi z Auth0 (rola + Action w flow post-login), gość dostaje `guest` w swoim tokenie. Reguły platformy leżą w `PlatformRules` i są wołane z `Decide`; limity wymagające spójności (aktywne i utworzone gry) idą przez granice DCB (`JoinGameDecisionState`, `CreateGameDecisionState`). W testach rolę podaje nagłówek `X-Test-Roles`.
- **Nazwy a zapisany JSON:** `GameCreatorId`, `PlayerOwnerId` mają `[JsonPropertyName("CreatorId"/"OwnerId")]`, więc eventy i snapshot w Neonie pozostają czytelne bez przebudowy (`StoredJsonCompatibilityTests`). Modele odczytu, front i bot nadal używają `OwnerId`.
- **Metadane eventów:** `UserName` i `CorrelationId` zapisują się dla nowych eventów, stare mają `NULL`.
- **Sprzątanie gier:** `StaleGameCleanupService` na Renderze działa wewnątrz procesu API, więc tylko gdy API nie śpi; po obudzeniu pierwszy przebieg startuje po ok. minucie.
### Integration Tests
| Plik | Opis |
|------|------|
| `IntegrationTests/SplendorApiFactory.cs` | WebApplicationFactory - konfiguracja testów z Testcontainers |
| `IntegrationTests/BasicTests.cs` | Podstawowe testy API (Swagger, Create Game) |
| `IntegrationTests/GameFlowTests.cs` | Przepływ gry, autoryzacja ruchu i zmiana deadline'u tury |
| `IntegrationTests/HybridConcurrencyTests.cs` | Routing do jednego strumienia, snapshoty oraz konflikty stream/DCB |
| `IntegrationTests/RateLimitingTests.cs` | Limit `GameActions`: 11. żądanie użytkownika w oknie -> 429, limit liczony per użytkownik |
| `IntegrationTests/ChooseNobleCommandHandlerTests.cs` | Integracja wyboru arystokraty |
| `IntegrationTests/TurnExpirationSchedulingTests.cs` | Scheduler → consumer → `ExpireTurnCommand` → `TurnExpired` |
| `IntegrationTests/TestAuthHandler.cs` | Fake authentication handler - czyta X-Test-User-Id header |
| `IntegrationTests/TestUserContext.cs` | DelegatingHandler ustawiający X-Test-User-Id per request |

#### Konfiguracja testów integracyjnych
- **PostgreSQL** (Testcontainers) - dla Marten Event Store i read modeli
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
| `src/app/core/interceptors/error.interceptor.ts` | Toast dla 400; 401 otwiera dialog ponownego logowania (`AuthService.requireLogin`). |
| `src/app/pages/games-list/` | Lista gier + tworzenie nowej; "Watch bot vs bot" tworzy grę `<nazwa> bot vs bot`, zaprasza bota i otwiera lobby (wyszarzone, gdy taka niezakończona gra już istnieje). |
| `src/app/pages/lobby/` | Lobby gry (dołączanie, start, przycisk Leave przy graczach; o możliwości wyjścia decyduje backend, błąd wraca jako toast) |
| `src/app/pages/game/` | Widok rozgrywki (rynek gemów, karty, gracze). Stan z SignalR jest renderowany dopiero po zakończeniu animacji (`receive()`); po własnych akcjach nie ma `refresh()`, stan przychodzi tylko przez SignalR. Po `status = Finished` overlay końca gry (`isFinished()`, `getStandings()`). |
| `src/app/pages/game/card/` | `CardComponent` (`app-card`): tło `assets/cards/card-<kolor>-<poziom>.webp` (rewers `card-back-<poziom>.webp`) z fallbackiem gradientem CSS; nakładka: punkty, bonus, koszt. |
| `src/app/core/services/game-animation.service.ts` | Animacje w DOM (Web Animations API): lot karty/arystokraty do panelu gracza, kropki żetonów (pobranie, płatność), pulsowanie. Elementy po `data-snap`, `data-player-id`, `data-player-gem`, `data-market-gem`. Skip (Esc), przełącznik zapisany w `localStorage`, `whenIdle()`. |
| `src/app/models/` | Interfejsy TS (GameView, GemCollection, Card, requests) |
| `src/environments/environment.ts` | Konfiguracja API URL |

## Model danych

### Identyfikatory (ważne!)
- **GameId**: `Guid` - identyfikator gry
- **OwnerId**: `string` - identyfikator użytkownika (z JWT `sub`); w komendach jako `Caller.UserId`, w stanie jako `PlayerOwnerId`
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

1. `GameCreated` - utworzenie gry przez CreatorId, z opcjonalną nazwą i czasem tury
2. `PlayerJoined` - dołączenie gracza (2-4 graczy)
3. `GameStarted` - rozpoczęcie (tasowanie talii, setup rynku)
4. `TurnStarted` + `TurnDeadlineStarted` - początek tury i zaplanowanie jej deadline'u
5. Akcja gracza:
   - `GemsTaken` - pobranie gemów (max 3 różne, 2 takie same przy rynku >= 4 lub 1 złoty)
   - `CardPurchased` + `CardRevealed` - kupno karty
6. Gdy gracz osiągnie ≥ 15 punktów prestiżu, gra trwa do końca bieżącej rundy.
7. Po domknięciu rundy `GameFinished` wskazuje zwycięzcę: najwięcej punktów prestiżu, a przy remisie mniej zakupionych kart.
8. W przeciwnym wypadku `TurnEnded` - koniec tury i powrót do punktu 4 (następny gracz).
9. Po przekroczeniu deadline'u scheduler wywołuje `ExpireTurnCommand`; aktualna tura emituje `TurnExpired`, a spóźnione wiadomości są ignorowane dzięki `TurnId`. Po przekroczeniu limitu żetonów lub `NobleSelectionRequired` powstaje świeży `TurnDeadlineStarted`; po jego upływie `ExpireTurnCommand` zwraca żetony automatycznie (najliczniejszy kolor, złoto na końcu) albo wybiera arystokratę (pierwszy po sortowaniu id).
10. `PlayerLeft` usuwa gracza z lobby, a `PlayerParticipationEnded` zwalnia jego aktywny udział po zakończeniu lub usunięciu gry.




## API Endpoints

| Metoda | Endpoint | Opis |
|---|---|---|
| POST | `/auth/guest` | Token gościa (60 min, limit aktywnych gości, rate limit po IP) |
| GET | `/games` | Lista gier: max 100 ostatnio zmienionych (`PlatformRules.MaxListedGames`); `includeDeleted` tylko dla admina |
| POST | `/games` | Tworzenie gry (opcjonalne `name` 1-40 znaków i `turnDurationSeconds` 30-300) |
| GET | `/games/{id}` | Aktualny stan gry |
| DELETE | `/games/{id}` | Usuwanie gry (tylko twórca) |
| POST | `/games/{id}/players` | Dołączanie gracza |
| DELETE | `/games/{id}/players/{playerId}` | Opuszczanie lobby |
| POST | `/games/{id}/invite` | Zapraszanie użytkownika do gry |
| POST | `/games/{id}/start` | Rozpoczęcie gry |
| POST | `/games/{id}/actions/take-gems` | Pobieranie żetonów |
| POST | `/games/{id}/actions/resolve-gem-limit` | Zwrot nadmiaru żetonów |
| POST | `/games/{id}/actions/buy-card` | Kupowanie karty |
| POST | `/games/{id}/actions/reserve-card` | Rezerwacja karty |
| POST | `/games/{id}/actions/choose-noble` | Wybór arystokraty |
| GET | `/games/{id}/version` | Wersja gry |
| GET | `/cards` | Definicje kart |
| GET | `/nobles` | Definicje arystokratów |

### Testy

```bash
dotnet test Splendor.UnitTests
dotnet test Splendor.IntegrationTests
dotnet test Splendor.UITests
```

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
- Async snapshot `SplendorGameState` pojedynczego strumienia gry, uzupełniony wąskimi boundary DCB (`JoinGameDecisionState`, `TurnClockState`) oraz czystymi regułami w `SplendorRules`.
- Pełna talia Splendor: 90 kart rozwoju na trzech poziomach.
- Rozgrywka dla 2-4 graczy: tworzenie gry, lobby, dołączanie graczy i tury.
- Pobieranie żetonów zgodnie z regułami gry:
  - trzy różne kolory;
  - dwa żetony tego samego koloru, gdy w rynku są co najmniej cztery;
  - limit 10 żetonów z obowiązkowym zwrotem nadmiaru.
- Kupowanie kart z uwzględnieniem stałych bonusów i złotych żetonów jako wildcardów.
- Rezerwacja kart: maksymalnie trzy na gracza; rezerwacja przyznaje złoty żeton, jeśli jest dostępny.
- Arystokraci: automatyczne przyznanie jednego dostępnego arystokraty albo wybór, gdy gracz kwalifikuje się do kilku.
- Zakończenie gry po domknięciu rundy, w której ktoś osiągnął co najmniej 15 punktów prestiżu.
- Read modele Marten: `GameSummaryView`, `SplendorBoardView`, `PlayerBoardView`; `GetGameQuery` mapuje planszę do kompatybilnego `GameView`. `UserStatsView` jest na razie nieużywanym placeholderem bez projekcji i endpointów.
- REST API, Swagger, JWT/Auth0, SignalR oraz MassTransit/RabbitMQ.
- Podstawowe metryki aplikacji i Martena przez `System.Diagnostics.Metrics` oraz eksport OpenTelemetry do lokalnego Aspire Dashboard.
- ETag/`304 Not Modified` dla `GET /games/{id}` i polling wersji gry.
- Role i reguły platformy: `Caller`, `PlatformRules`, admin usuwa cudze gry, limity (max 2 aktywne i max 2 utworzone, nieukończone gry; dla roli `bot` 40 aktywnych i 20 utworzonych), walidacja wejścia przez value objecty `PlayerName` i `UserId` oraz limit body 16 KB.
- Metadane eventów (`UserName`, `CorrelationId`) oraz sprzątanie gier: logiczne usuwanie po 48 h nieaktywności (fizyczne kasowanie wyłączone).
- Frontend: `ToastService` (komunikaty 400 z API, ruchy przeciwników z listy powiadomień wysyłanej obok `GameView` przez SignalR; przy `gems_taken` z listą żetonów), toasty u góry na środku.
- Frontend: komponent karty z obrazkami, animacje (lot karty, płatność, żetony, arystokrata), Skip/Esc i przełącznik animacji; pobranie 1-3 żetonów różnych kolorów; `getGame` obsługuje 304 jako stan z cache.
- Frontend: przycisk "Replay last move" (`lastMove` = stan sprzed ruchu + powiadomienia; `replayLastMove()` renderuje stary stan i puszcza te same animacje, Esc kończy powtórkę).
- Frontend: ekran końca gry (overlay ze zwycięzcą i tabelą końcową; `data-testid`: `game-over-overlay`, `winner-title`, `view-board-btn`, `back-to-games-btn`).
- Limit 10 utworzonych gier łącznie dla gościa (create + delete nie omija limitu otwartych gier) oraz opuszczanie lobby z frontu.
- Nazwa gry i czas tury (30-300 s) jako pola `GameCreated`; czas tury czyta `TurnClockState`.
- Automatyczne rozstrzyganie wygasłej tury z oczekującym zwrotem żetonów lub wyborem arystokraty (świeży deadline na fazę oczekiwania).
- Dialog ponownego logowania przy 401 (gość, wklejony token, przeładowanie strony po zalogowaniu).
- Testy jednostkowe decyzji gameplayu w `Splendor.UnitTests`.
- Testy integracyjne z Testcontainers oraz testy UI Selenium z Page Object Model.


### Do zrobienia

**Frontend:**
- [x] Nazywanie gry (przy tworzeniu)
- [ ] Wyświetlanie nazw graczy w games-list

**Bot worker:**
- [ ] Rozważyć `IBotGameRegistry` jako cache gier i playerów kontrolowanych przez bota; cache nie może być źródłem prawdy i musi umieć odbudować stan po restarcie workera.

**Obserwowalność i wydajność:**
- [x] Podstawowe metryki komend i operacji Martena.
- [x] Eksport metryk i trace’ów przez OpenTelemetry do lokalnego Aspire Dashboard.
- [x] Uproszczony benchmark DCB/live/snapshot w `Splendor.LoadTests` dostarczył przesłanki do przyjęcia architektury hybrydowej.

## Konwencje kodu

- Eventy jako `record` w `GameEvents.cs`
- Marten decision state stosuje eventy przez metody `Apply(Event)`
- Czyste reguły domenowe trzymać w `Splendor.Domain.Rules`
- Komendy obsługiwane przez MediatR handlery
- Projekcje Marten aktualizują dokumentowe read modele
- Elementy UI testowalne oznaczane atrybutem `data-testid` w szablonach Angular
- Dane ustalane przy tworzeniu gry są polami `GameCreated` z wartościami domyślnymi, żeby stare eventy się deserializowały
- Liczby reguł gry (limit żetonów, próg końca gry) żyją w `SplendorRules`, nie jako literały
- Decyzje architektoniczne zapisujemy jako ADR w `docs/adr/` (po angielsku, numerowane; sekcje: Context, Decision, Alternatives, Consequences). Zmiana decyzji to nowy ADR i status starego "Superseded".
