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
        private readonly ICardDefinitionsProvider _cardDefinitionsProvider;
        private readonly ILogger<BotTurnProcessor> _logger;

        public BotTurnProcessor(IGameApiClient gameApi, IBotStrategy strategy, IConfiguration configuration, IAccessTokenProvider tokenProvider, ICardDefinitionsProvider cardDefinitionsProvider, ILogger<BotTurnProcessor> logger)
        {
            _gameApi = gameApi;
            _strategy = strategy;
            _tokenProvider = tokenProvider;
            _cardDefinitionsProvider = cardDefinitionsProvider;
            _logger = logger;
        }

        public async Task ProcessAsync(Guid gameId, long messageVersion, CancellationToken cancellationToken)
        {
            var botUserId = await _tokenProvider.GetUserIdAsync(cancellationToken);
            var game = await _gameApi.GetGameAsync(gameId, cancellationToken);

            if (game is null || game.Status != "Started")
            {
                return;
            }

            var botPlayer = game.Players.FirstOrDefault(player => player.OwnerId == botUserId);
            if (botPlayer is null || game.CurrentPlayerId != botPlayer.Id)
            {
                return;
            }

            var cards = await _cardDefinitionsProvider.GetCardsAsync(cancellationToken);
            var action = _strategy.ChooseMove(game, botPlayer.Id, cards);

            if (action is null)
            {
                return;
            }

            try
            {
                switch (action)
                {
                    case BuyCardAction buy:
                        await _gameApi.BuyCardAsync(gameId, new BuyCardRequest(botPlayer.Id, buy.CardId), cancellationToken);
                        break;

                    case TakeGemsAction gems:
                        await _gameApi.TakeGemsAsync(gameId, new TakeGemsRequest(botPlayer.Id, gems.Diamond, gems.Sapphire, gems.Emerald, gems.Ruby, gems.Onyx, gems.Gold), cancellationToken);
                        break;

                    case ReserveCardAction card:
                        await _gameApi.ReserveCardAsync(gameId, new ReserveCardRequest(botPlayer.Id, card.CardId), cancellationToken);
                        break;

                    case ChooseNobleAction noble:
                        await _gameApi.ChooseNobleAsync(gameId, new ChooseNobleRequest(botPlayer.Id, noble.NobleId), cancellationToken);
                        break;
                    default:
                        break;
                }

            }
            catch (Exception)
            {
            }
        }
    }
}