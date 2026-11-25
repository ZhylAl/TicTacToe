using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicTacToe.Application.Interfaces;
using TicTacToe.Contracts.DTO;

namespace TicTacToe.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly IGameService _gameService;
        public GameController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateGameRequest request, CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User identifier claim is missing or invalid.");
            }

            var gameResponse = await _gameService.CreateGameAsync(userId, request, cancellationToken);
            return Ok(gameResponse);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGame(Guid id, CancellationToken cancellationToken)
        {
            var gameResponse = await _gameService.GetGameAsync(id, cancellationToken);
            if (gameResponse == null)
            {
                return NotFound();
            }

            return Ok(gameResponse);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
        {
            var history = await _gameService.GetRecentGamesAsync(10, cancellationToken);
            return Ok(history);
        }
    }
}
