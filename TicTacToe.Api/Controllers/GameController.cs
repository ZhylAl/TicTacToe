using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicTacToe.Application.Interfaces;

namespace TicTacToe.Api.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString!);

            var gameResponse = await _gameService.CreateGameAsync(userId, cancellationToken);
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
    }
}
