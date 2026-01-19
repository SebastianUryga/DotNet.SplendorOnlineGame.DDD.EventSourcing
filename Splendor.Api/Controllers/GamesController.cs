using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splendor.Application.Commands;
using Splendor.Application.Queries;
using Splendor.Application.Common.Interfaces;

namespace Splendor.Api.Controllers;

[Authorize]
[ApiController]
[Route("games")]
public class GamesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public GamesController(IMediator mediator, Splendor.Application.Common.Interfaces.ICurrentUserService currentUserService)
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
    public async Task<IActionResult> CreateGame([FromBody] CreateGameCommand command)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var gameId = await _mediator.Send(command with { OwnerId = userId });
        return CreatedAtAction(nameof(GetGame), new { gameId }, new { id = gameId });
    }

    /// <summary>
    /// Joins a player to an existing game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="command">The player details.</param>
    /// <response code="200">If the player successfully joined the game.</response>
    /// <response code="400">If there is a GameId mismatch or business logic failure.</response>
    [HttpPost("{gameId}/players")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinGame(Guid gameId, [FromBody] JoinGameCommand command)
    {
        if (gameId != command.GameId) return BadRequest("GameId mismatch");
        
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(command with { OwnerId = userId });
        return Ok();
    }

    /// <summary>
    /// Starts the game after players have joined.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <response code="200">If the game started successfully.</response>
    [HttpPost("{gameId}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartGame(Guid gameId)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _mediator.Send(new StartGameCommand(gameId, userId));
        return Ok();
    }

    /// <summary>
    /// Takes gems from the market for a player.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="command">The gem selection details.</param>
    /// <response code="200">If the gems were successfully taken.</response>
    /// <response code="400">If there is a GameId mismatch or invalid gem combination.</response>
    [HttpPost("{gameId}/actions/take-gems")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TakeGems(Guid gameId, [FromBody] TakeGemsCommand command)
    {
         if (gameId != command.GameId) return BadRequest("GameId mismatch");

        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Pass 'userId' as the OwnerId, but keep 'PlayerId' from the command body
        await _mediator.Send(command with { OwnerId = userId });
        return Ok();
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
    public async Task<IActionResult> GetGame(Guid gameId)
    {
        var game = await _mediator.Send(new GetGameQuery(gameId));
        if (game == null) return NotFound();
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
}
