using Microsoft.EntityFrameworkCore;
using TicTacToe.Contracts.DTO;
using ContractEnums = TicTacToe.Contracts.Enums;
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
        private readonly IBotPlayer _botPlayer;
        public GameService(ApplicationDbContext context, IGameEngine gameEngine, IBotPlayer botPlayer)
        {
            _context = context;
            _gameEngine = gameEngine;
            _botPlayer = botPlayer;
        }

        public async Task<GameResponse> CreateGameAsync(Guid playerXId, CreateGameRequest request, CancellationToken cancellationToken)
        {
            var game = new Game
            {
                PlayerXId = playerXId,
                Mode = (Core.Enums.GameMode)request.Mode
            };
            switch (request.Mode)
            {
                case ContractEnums.GameMode.HotSeat:
                    game.PlayerOId = Guid.NewGuid();
                    game.Status = GameStatus.InProgress;
                    break;
                case ContractEnums.GameMode.VsBot:
                    game.PlayerOId = Guid.NewGuid();
                    game.Status = GameStatus.InProgress;
                    game.BotDifficulty = request.Difficulty.HasValue
                        ? (Core.Enums.BotDifficulty)request.Difficulty.Value
                        : Core.Enums.BotDifficulty.Easy;
                    break;
                case ContractEnums.GameMode.PvpOnline:
                default:
                    game.Status = GameStatus.WaitingForOpponent;
                    break;
            }
            _context.Games.Add(game);
            await _context.SaveChangesAsync(cancellationToken);
            return ToResponse(game);
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
                return ToResponse(game);
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

            return ToResponse(game);
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

            return ToResponse(game);
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
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Guest", cancellationToken);

            return games.Select(g =>
            {
                var playerX = users.GetValueOrDefault(g.PlayerXId, "Guest");

                string playerO;
                if (g.Mode == Core.Enums.GameMode.VsBot)
                {
                    playerO = $"Bot ({g.BotDifficulty ?? Core.Enums.BotDifficulty.Easy})";
                }
                else if (g.Mode == Core.Enums.GameMode.HotSeat)
                {
                    playerO = "Friend (Hot-Seat)";
                }
                else
                {
                    playerO = g.PlayerOId.HasValue
                        ? users.GetValueOrDefault(g.PlayerOId.Value, "Guest")
                        : "Waiting...";
                }

                string result;
                if (g.WinnerId.HasValue)
                {
                    var winnerName = g.WinnerId == g.PlayerXId ? playerX : playerO;
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

        public async Task<GameResponse?> MakeBotMoveAsync(Guid gameId, CancellationToken cancellationToken)
        {
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);

            if (game == null ||
                game.Mode != Core.Enums.GameMode.VsBot ||
                game.Status != GameStatus.InProgress ||
                game.CurrentTurn != CellState.O ||
                !game.PlayerOId.HasValue)
            {
                return null;
            }
            var difficulty = game.BotDifficulty ?? Core.Enums.BotDifficulty.Easy;

            int botMove = _botPlayer.GetMove(game.Board, CellState.O, difficulty);

            var moveResult = _gameEngine.MakeMove(game, game.PlayerOId.Value, botMove);
            if (!moveResult.IsSuccess)
            {
                return null;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return ToResponse(game);
        }

        private static GameResponse ToResponse(Game game)
        {
            return new GameResponse(
                game.Id,
                game.PlayerXId,
                game.PlayerOId,
                game.Board.Select(c => (ContractEnums.CellState)c).ToArray(),
                (ContractEnums.CellState)game.CurrentTurn,
                (ContractEnums.GameStatus)game.Status,
                game.WinnerId
            );
        }
    }
}
