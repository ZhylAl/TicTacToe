using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.DTOs;
using TicTacToe.Application.Interfaces;
using TicTacToe.Core.Entities;
using TicTacToe.Core.Enums;
using TicTacToe.Core.Interfaces;
using TicTacToe.Infrastructure.Data;

namespace TicTacToe.Infrastructure.Services
{
    public class GameService : IGameService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGameEngine _gameEngine;

        public GameService(ApplicationDbContext Context, IGameEngine gameEngine)
        {
            _context = Context;
            _gameEngine = gameEngine;
        }

        public async Task<GameResponse> CreateGameAsync(Guid playerXId, CancellationToken cancellationToken)
        {
            var game = new Game
            {
                PlayerXId = playerXId,
                Status = GameStatus.WaitingForOpponent
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync(cancellationToken);

            return new GameResponse
            (
                Id: game.Id,
                PlayerX: game.PlayerXId,
                PlayerO: game.PlayerOId,
                Board: game.Board,
                CurrentTurn: game.CurrentTurn,
                Status: game.Status
            );
        }

        public async Task<GameResponse?> GetGameAsync(Guid gameId, CancellationToken cancellationToken)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);

            if (game == null)
            {
                return null;
            }
            else
            {
                return new GameResponse
                (
                    Id: game.Id,
                    PlayerX: game.PlayerXId,
                    PlayerO: game.PlayerOId,
                    Board: game.Board,
                    CurrentTurn: game.CurrentTurn,
                    Status: game.Status,
                    WinnerId: game.WinnerId
                );
            }
        }

        public async Task<GameResponse> JoinGameAsync(Guid gameId, Guid playerOId, CancellationToken cancellationToken)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
            if (game == null)
            {
                throw new Exception("Game not found."); //throw or return null?
            }

            if (game.Status != GameStatus.WaitingForOpponent)
            {
                throw new Exception("Game already in progress or finished."); //throw or return null?
            }

            game.PlayerOId = playerOId;
            game.Status = GameStatus.InProgress;
            game.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new GameResponse
            (
                Id: game.Id,
                PlayerX: game.PlayerXId,
                PlayerO: game.PlayerOId,
                Board: game.Board,
                CurrentTurn: game.CurrentTurn,
                Status: game.Status
            );
        }

        public async Task<GameResponse> MakeMoveAsync(Guid gameId, Guid playerId, int position, CancellationToken cancellationToken)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
            if (game == null)
            {
                throw new Exception("Game not found."); //throw or return null?
            }

            var moveResult = _gameEngine.MakeMove(game, playerId, position);
            if (!moveResult.IsSuccess)
            {
                throw new Exception(moveResult.ErrorMessage); //throw or return null?
            }

           await _context.SaveChangesAsync(cancellationToken);

            return new GameResponse
            (
                Id: game.Id,
                PlayerX: game.PlayerXId,
                PlayerO: game.PlayerOId,
                Board: game.Board,
                CurrentTurn: game.CurrentTurn,
                Status: game.Status,
                WinnerId: game.WinnerId
            );
        }

        public async Task<IReadOnlyList<GameHistoryItemResponse>> GetRecentGamesAsync(int count, CancellationToken cancellationToken)
        {
            var games = await _context.Games
                .OrderByDescending(g => g.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);

            if (games.Count == 0)
            {
                return Array.Empty<GameHistoryItemResponse>();
            }

            var userIds = games
                .Select(g => g.PlayerXId)
                .Concat(games.Where(g => g.PlayerOId.HasValue).Select(g => g.PlayerOId!.Value))
                .Distinct() 
                .ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Unknown", cancellationToken);

            return games.Select(g =>
            {
                var playerX = users.GetValueOrDefault(g.PlayerXId, "Unknown");
                var playerO = g.PlayerOId.HasValue ? users.GetValueOrDefault(g.PlayerOId.Value, "Unknown") : "Waiting...";

                string result;
                if (g.WinnerId.HasValue)
                {
                    var winnerName = users.GetValueOrDefault(g.WinnerId.Value, "Unknown");
                    result = $"{winnerName} won";
                }
                else if (g.Status == GameStatus.Finished)
                {
                    result = "Draw";
                }
                else
                {
                    result = g.Status.ToString();
                }

                return new GameHistoryItemResponse(
                    g.Id,
                    playerX,
                    playerO,
                    g.Status.ToString(),
                    result,
                    g.CreatedAt
                );
            }).ToList();
        }
    }
}
