using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splendor.Application.Commands;
using Splendor.Application.Queries;
using Splendor.Application.Common.Interfaces;
using Splendor.Application.ReadModels;
using Splendor.Contracts.Games;


namespace Splendor.Api.Controllers;

[Authorize]
[ApiController]
[Route("games")]
public class GamesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public GamesController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }


    /// <summary>
    /// Creates a new game instance.
    /// </summary>
    /// <param name="command">The create game parameters.</param>
    /// <returns>The newly created game ID.</returns>
    /// <response code="201">Returns the newly created game ID.</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var gameId = await _mediator.Send(new CreateGameCommand { OwnerId = userId });
        return CreatedAtAction(nameof(GetGame), new { gameId }, new { id = gameId });
    }

    /// <summary>
    /// Joins a player to an existing game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="request">The player details.</param>
    /// <response code="200">If the player successfully joined the game.</response>
    /// <response code="400">If there is a GameId mismatch or business logic failure.</response>
    [HttpPost("{gameId}/players")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinGame(Guid gameId, [FromBody] JoinGameRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new JoinGameCommand 
        { 
            GameId = gameId, 
            OwnerId = userId, 
            Name = request.Name 
        });
        return Ok();
    }

    /// <summary>
    /// Invites a player to join an existing game.
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("{gameId}/invite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InvitePlayer(Guid gameId, [FromBody] InvitePlayerRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new InvitePlayerCommand
        {
            GameId = gameId,
            OwnerId = userId,
            InviteeId = request.InviteeId
        });

        return Ok();
    }

    /// <summary>
    /// Starts the game after players have joined.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <response code="200">If the game started successfully.</response>
    [HttpPost("{gameId}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartGame(Guid gameId, [FromBody] StartGameRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new StartGameCommand { GameId = gameId, OwnerId = userId });
        return Ok();
    }

    /// <summary>
    /// Takes gems from the market for a player.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="request">The gem selection details.</param>
    /// <response code="200">If the gems were successfully taken.</response>
    /// <response code="400">If there is a GameId mismatch or invalid gem combination.</response>
    [HttpPost("{gameId}/actions/take-gems")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TakeGems(Guid gameId, [FromBody] TakeGemsRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new TakeGemsCommand 
        { 
            GameId = gameId,
            OwnerId = userId,
            PlayerId = request.PlayerId,
            Diamond = request.Diamond,
            Sapphire = request.Sapphire,
            Emerald = request.Emerald,
            Ruby = request.Ruby,
            Onyx = request.Onyx,
            Gold = request.Gold
        });
        return Ok();
    }

    /// <summary>
    /// Purchases a card from the market for a player.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="request">The purchase details.</param>
    /// <response code="200">If the card was successfully purchased.</response>
    /// <response code="400">If the card cannot be purchased or player mismatch.</response>
    [HttpPost("{gameId}/actions/buy-card")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BuyCard(Guid gameId, [FromBody] BuyCardRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new BuyCardCommand 
        { 
            GameId = gameId,
            OwnerId = userId,
            PlayerId = request.PlayerId,
            CardId = request.CardId
        });
        return Ok();
    }

    /// <summary>
    /// Retrieves a list of all game cards definitions.
    /// </summary>
    /// <returns>A list of card definitions.</returns>
    [HttpGet("/cards")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<Splendor.Domain.ValueObjects.Card>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCards()
    {
        var cards = await _mediator.Send(new GetCardsQuery());
        return Ok(cards);
    }

    /// <summary>
    /// Retrieves a list of all game nobles.
    /// </summary>
    /// <returns>A list of noble definitions.</returns>
    [HttpGet("/nobles")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<Splendor.Domain.ValueObjects.Noble>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNobles()
    {
        var nobles = await _mediator.Send(new GetNoblesQuery());
        return Ok(nobles);
    }

    /// <summary>
    /// Retrieves a list of all games.
    /// </summary>
    /// <param name="includeDeleted">Whether to include deleted games in the result.</param>
    /// <returns>A list of game summaries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GameSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGames([FromQuery] bool includeDeleted = false)
    {
        var games = await _mediator.Send(new GetGamesQuery(includeDeleted));
        return Ok(games);
    }
    
    /// <summary>
    /// Deletes a game.
    /// </summary>
    /// <param name="id">The unique identifier of the game.</param>
    /// <response code="204">If the game was successfully deleted.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGame(Guid id)
    {
        await _mediator.Send(new DeleteGameCommand(id));
        return NoContent();
    }


    /// <summary>
    /// Retrieves the current state of a game.
    /// </summary>

    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>The game read model.</returns>
    /// <response code="200">Returns the game state.</response>
    /// <response code="404">If the game was not found.</response>
    [HttpGet("{gameId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetGame(Guid gameId)
    {
        var game = await _mediator.Send(new GetGameQuery(gameId));
        if (game == null) return NotFound();

        // ETag/304 Support
        var etag = $"\"{game.Version}\"";
        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;
        return Ok(game);
    }
    
    /// <summary>
    /// Retrieves the event history for a specific game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>A list of events that occurred in the game.</returns>
    /// <response code="200">Returns the event history.</response>
    /// <response code="404">If the game was not found.</response>
    [HttpGet("{gameId}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid gameId)
    {
        var events = await _mediator.Send(new GetGameHistoryQuery(gameId));
        if (events == null) return NotFound();
        return Ok(events);
    }

    [HttpPost("{gameId}/actions/reserve-card")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReserveCard(Guid gameId, [FromBody] ReserveCardRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new ReserveCardCommand
        {
            GameId = gameId,
            OwnerId = userId,
            PlayerId = request.PlayerId,
            CardId = request.CardId
        });

        return Ok();
    }

    /// <summary>
    /// Retrieves the list of actions currently available to the active player.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>A list of available actions.</returns>
    /// <response code="200">Returns the available actions.</response>
    /// <response code="404">If the game was not found.</response>
    [HttpGet("{gameId}/available-actions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableActions(Guid gameId)
    {
        var actions = await _mediator.Send(new GetAvailableActionsQuery(gameId));
        if (actions == null) return NotFound();
        return Ok(actions);
    }
    /// <summary>
    /// Retrieves the current version of the game state.
    /// Useful for lightweight polling to check for updates.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>The version number.</returns>
    [HttpGet("{gameId}/version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(Guid gameId)
    {
        var version = await _mediator.Send(new GetGameVersionQuery(gameId));
        if (version == null) return NotFound();
        return Ok(new { Version = version });
    }

    /// <summary>
    /// Resolves gem limit by returning specified gems for a player.
    /// </summary>
    [HttpPost("{gameId}/actions/resolve-gem-limit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveGemLimit(Guid gameId, [FromBody] ResolveGemLimitRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new ResolveGemLimitCommand
        {
            GameId = gameId,
            OwnerId = userId,
            PlayerId = request.PlayerId,
            Diamond = request.Diamond,
            Sapphire = request.Sapphire,
            Emerald = request.Emerald,
            Ruby = request.Ruby,
            Onyx = request.Onyx,
            Gold = request.Gold
        });

        return Ok();
    }

    [HttpPost("{gameId}/actions/choose-noble")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChooseNoble(Guid gameId, [FromBody] ChooseNobleRequest request)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new ChooseNobleCommand
        {
            GameId = gameId,
            OwnerId = userId,
            PlayerId = request.PlayerId,
            NobleId = request.NobleId
        });

        return Ok();
    }
}
public record StartGameRequest();
