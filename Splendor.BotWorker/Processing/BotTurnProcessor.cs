using Splendor.BotWorker.Api;
using Splendor.BotWorker.Authentication;
using Splendor.BotWorker.Cards;
using Splendor.BotWorker.Strategies;
using Splendor.Contracts.Games;

namespace Splendor.BotWorker.Processing
{
    public class BotTurnProcessor : IBotTurnProcessor
    {
        private readonly IGameApiClient _gameApi;
        private readonly IBotStrategy _strategy;
        private readonly IAccessTokenProvider _tokenProvider;
        private readonly IGameDefinitionsProvider _gameDefinitionsProvider;
        private readonly ILogger<BotTurnProcessor> _logger;

        public BotTurnProcessor(IGameApiClient gameApi, IBotStrategy strategy, IAccessTokenProvider tokenProvider, IGameDefinitionsProvider gameDefinitionsProvider, ILogger<BotTurnProcessor> logger)
        {
            _gameApi = gameApi;
            _strategy = strategy;
            _tokenProvider = tokenProvider;
            _gameDefinitionsProvider = gameDefinitionsProvider;
            _logger = logger;
        }

        public async Task ProcessAsync(Guid gameId, long messageVersion, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing turn for game {GameId} v{Version}", gameId, messageVersion);

            var botUserId = await _tokenProvider.GetUserIdAsync(cancellationToken);
            var game = await _gameApi.GetGameAsync(gameId, cancellationToken);
            if (game is null)
            {
                _logger.LogWarning("Game {GameId} not available", gameId);
                return;
            }

            if (game.Version < messageVersion)
            {
                _logger.LogInformation("Processing for game {GameId}: message version {MessageVersion} is newer than game version {GameVersion}", gameId, messageVersion, game.Version);
            }

            if (game.Status != "Started")
            {
                _logger.LogInformation("Game {GameId} is not in 'Started' status (Current status: {Status})", gameId, game.Status);
                return;
            }

            var botPlayerId = game.Players.FirstOrDefault(player => player.OwnerId == botUserId && player.Id == game.CurrentPlayerId)?.Id;
            if (string.IsNullOrEmpty(botPlayerId))
            {
                _logger.LogDebug("Skipping processing for game {GameId}: bot player not found or not current player", gameId);
                return;
            }

            var cards = await _gameDefinitionsProvider.GetCardsAsync(cancellationToken);
            var action = _strategy.ChooseMove(game, botPlayerId, cards);

            if (action is null)
            {
                _logger.LogInformation("No action chosen for player {PlayerId} in game {GameId}", botPlayerId, gameId);
                return;
            }

            try
            {
                switch (action)
                {
                    case BuyCardAction buy:
                        await _gameApi.BuyCardAsync(gameId, new BuyCardRequest(botPlayerId, buy.CardId), cancellationToken);
                        _logger.LogInformation("BuyCard executed: CardId={CardId} for Player={PlayerId}", buy.CardId, botPlayerId);
                        break;

                    case TakeGemsAction gems:
                        await _gameApi.TakeGemsAsync(gameId, new TakeGemsRequest(botPlayerId, gems.Diamond, gems.Sapphire, gems.Emerald, gems.Ruby, gems.Onyx, gems.Gold), cancellationToken);
                        _logger.LogInformation("TakeGems executed for Player={PlayerId} (D:{Diamond} S:{Sapphire} E:{Emerald} R:{Ruby} O:{Onyx} G:{Gold})",
                            botPlayerId, gems.Diamond, gems.Sapphire, gems.Emerald, gems.Ruby, gems.Onyx, gems.Gold);
                        break;

                    case ReserveCardAction card:
                        await _gameApi.ReserveCardAsync(gameId, new ReserveCardRequest(botPlayerId, card.CardId), cancellationToken);
                        _logger.LogInformation("ReserveCard executed: CardId={CardId} for Player={PlayerId}", card.CardId, botPlayerId);
                        break;

                    case ChooseNobleAction noble:
                        await _gameApi.ChooseNobleAsync(gameId, new ChooseNobleRequest(botPlayerId, noble.NobleId), cancellationToken);
                        _logger.LogInformation("ChooseNoble executed: NobleId={NobleId} for Player={PlayerId}", noble.NobleId, botPlayerId);
                        break;
                    default:
                        _logger.LogWarning("Unknown action type {ActionType} for game {GameId}", action.GetType().Name, gameId);

                        break;
                }

            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while processing turn for game {GameId}", gameId);
            }
        }
    }
}
