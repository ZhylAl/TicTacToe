using System;
using System.Linq;
using TicTacToe.Core.Entities;
using TicTacToe.Core.Enums;
using TicTacToe.Core.Interfaces;
using TicTacToe.Core.Models;

namespace TicTacToe.Core.Services
{
    public class GameEngine : IGameEngine
    {
        private static readonly int[][] WinConditions = new int[][]
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
            var validationError = ValidateMove(game, playerId, position);
            if (validationError != null)
            {
                return MoveResult.Failure(validationError);
            }

            // Apply move
            game.Board[position] = game.CurrentTurn;
            game.LastUpdatedAt = DateTime.UtcNow;

            // Check game outcome
            if (CheckWin(game.Board, game.CurrentTurn))
            {
                return CompleteGameWithWinner(game);
            }

            if (IsBoardFull(game.Board))
            {
                return CompleteGameWithDraw(game);
            }

            SwitchTurn(game);
            return MoveResult.Success(false);
        }

        private static string? ValidateMove(Game game, Guid playerId, int position)
        {
            if (game.Status != GameStatus.InProgress || game.PlayerOId == null)
                return "Game is not in progress.";

            if (position < 0 || position > 8)
                return "Invalid position. Must be between 0 and 8.";

            if (game.Board[position] != CellState.Empty)
                return "Cell is already occupied.";

            if (game.Mode == GameMode.HotSeat)
            {
                if (playerId != game.PlayerXId)
                    return "Only the game creator can make moves in HotSeat mode.";
            }
            else
            {
                if (playerId != game.PlayerXId && playerId != game.PlayerOId)
                    return "Player is not part of this game.";

                var currentPlayerId = game.CurrentTurn == CellState.X ? game.PlayerXId : game.PlayerOId.Value;
                if (currentPlayerId != playerId)
                    return "It's not the player's turn.";
            }

            return null;
        }

        private static MoveResult CompleteGameWithWinner(Game game)
        {
            var winnerId = game.CurrentTurn == CellState.X ? game.PlayerXId : game.PlayerOId!.Value;

            game.WinnerId = winnerId;
            game.FinishedAt = DateTime.UtcNow;
            game.Status = GameStatus.Finished;

            return MoveResult.Success(true, winnerId);
        }

        private static MoveResult CompleteGameWithDraw(Game game)
        {
            game.FinishedAt = DateTime.UtcNow;
            game.Status = GameStatus.Finished;

            return MoveResult.Success(true);
        }

        private static bool IsBoardFull(CellState[] board)
        {
            return board.All(cell => cell != CellState.Empty);
        }

        private static void SwitchTurn(Game game)
        {
            game.CurrentTurn = game.CurrentTurn == CellState.X ? CellState.O : CellState.X;
        }

        private static bool CheckWin(CellState[] board, CellState player)
        {
            foreach (var condition in WinConditions)
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