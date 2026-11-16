using TicTacToe.Core.Entities;
using TicTacToe.Core.Enums;
using TicTacToe.Core.Interfaces;
using TicTacToe.Core.Models;

namespace TicTacToe.Core.Services
{
    public class GameEngine : IGameEngine
    {
        private static readonly int[][] winConditions = new int[][]
        {
            new int[] { 0, 1, 2 },
            new int[] { 3, 4, 5 },
            new int[] { 6, 7, 8 },
            new int[] { 0, 3, 6 },
            new int[] { 1, 4, 7 },
            new int[] { 2, 5, 8 },
            new int[] { 0, 4, 8 },
            new int[] { 2, 4, 6 }
        };

        public MoveResult MakeMove(Game game, Guid playerId, int position)
        {
            // Validation
            if (game.Status != GameStatus.InProgress || game.PlayerOId == null)
                return MoveResult.Failure("Game is not in progress.");

            if (position < 0 || position > 8)
                return MoveResult.Failure("Invalid position. Must be between 0 and 8.");

            if (game.Board[position] != CellState.Empty)
                return MoveResult.Failure("Cell is already occupied.");

            if (game.Mode == GameMode.HotSeat)
            {
                // In HotSeat mode, only the game creator (PlayerX) can make moves
                if (playerId != game.PlayerXId)
                    return MoveResult.Failure("Only the game creator can make moves in HotSeat mode.");
            }
            else
            {
                // In normal online mode (PvP or vs Bot):
                if (playerId != game.PlayerXId && playerId != game.PlayerOId)
                    return MoveResult.Failure("Player is not part of this game.");
                Guid currentPlayerId = game.CurrentTurn == CellState.X ? game.PlayerXId : game.PlayerOId.Value;
                if (currentPlayerId != playerId)
                    return MoveResult.Failure("It's not the player's turn.");
            }

            // Make the move
            game.Board[position] = game.CurrentTurn;
            game.LastUpdatedAt = DateTime.UtcNow;

            // Check for a win or draw
            if (CheckWin(game.Board, game.CurrentTurn))
            {
                Guid winnerId = game.CurrentTurn == CellState.X ? game.PlayerXId : game.PlayerOId.Value;

                game.WinnerId = winnerId;
                game.FinishedAt = DateTime.UtcNow;
                game.Status = GameStatus.Finished;
                return MoveResult.Success(true, winnerId);
            }
            
            if (game.Board.All(cell => cell != CellState.Empty))
            {
                game.FinishedAt = DateTime.UtcNow;
                game.Status = GameStatus.Finished;
                return MoveResult.Success(true);
            }

            // Switch turns
            game.CurrentTurn = game.CurrentTurn == CellState.X ? CellState.O : CellState.X;

            return MoveResult.Success(false);
        }

        private bool CheckWin(CellState[] board, CellState player)
        {
            foreach (var condition in winConditions)
            {
                if (board[condition[0]] == player &&
                    board[condition[1]] == player &&
                    board[condition[2]] == player)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
