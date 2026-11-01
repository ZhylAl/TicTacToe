using TicTacToe.Core.Enums;
using TicTacToe.Core.Interfaces;

namespace TicTacToe.Core.Services;

public class BotPlayer : IBotPlayer
{
    private static readonly int[][] WinConditions =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // Rows
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // Columns
        [0, 4, 8], [2, 4, 6]             // Diagonals
    ];

    private readonly Random _random = new();

    public int GetMove(CellState[] board, CellState botSymbol, BotDifficulty difficulty)
    {
        var availableMoves = GetAvailableMoves(board);
        if (availableMoves.Count == 0)
        {
            throw new InvalidOperationException("No available moves on the board.");
        }

        return difficulty switch
        {
            BotDifficulty.Easy => GetRandomMove(availableMoves),
            BotDifficulty.Medium => GetMediumMove(board, availableMoves, botSymbol),
            BotDifficulty.Hard => GetBestMoveMinimax(board, botSymbol),
            _ => GetRandomMove(availableMoves)
        };
    }

    // --- Easy: purely random move ---
    private int GetRandomMove(List<int> availableMoves)
    {
        return availableMoves[_random.Next(availableMoves.Count)];
    }

    // --- Medium: heuristic (win if possible, block opponent, take center) ---
    private int GetMediumMove(CellState[] board, List<int> availableMoves, CellState botSymbol)
    {
        var opponentSymbol = botSymbol == CellState.X ? CellState.O : CellState.X;

        // 1. Can bot win immediately?
        foreach (var move in availableMoves)
        {
            board[move] = botSymbol;
            var wins = CheckWin(board, botSymbol);
            board[move] = CellState.Empty;
            if (wins) return move;
        }

        // 2. Can opponent win immediately? Block them!
        foreach (var move in availableMoves)
        {
            board[move] = opponentSymbol;
            var opponentWins = CheckWin(board, opponentSymbol);
            board[move] = CellState.Empty;
            if (opponentWins) return move;
        }

        // 3. Take center if available
        if (availableMoves.Contains(4))
        {
            return 4;
        }

        // 4. Otherwise, pick a random available move
        return GetRandomMove(availableMoves);
    }

    // --- Hard: Unbeatable Minimax Algorithm ---
    private int GetBestMoveMinimax(CellState[] board, CellState botSymbol)
    {
        var opponentSymbol = botSymbol == CellState.X ? CellState.O : CellState.X;
        var availableMoves = GetAvailableMoves(board);

        int bestScore = int.MinValue;
        int bestMove = availableMoves[0];

        foreach (var move in availableMoves)
        {
            board[move] = botSymbol;
            int score = Minimax(board, 0, false, botSymbol, opponentSymbol);
            board[move] = CellState.Empty;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int Minimax(CellState[] board, int depth, bool isMaximizing, CellState botSymbol, CellState opponentSymbol)
    {
        if (CheckWin(board, botSymbol)) return 10 - depth;
        if (CheckWin(board, opponentSymbol)) return depth - 10;
        if (board.All(c => c != CellState.Empty)) return 0; // Draw

        var availableMoves = GetAvailableMoves(board);

        if (isMaximizing)
        {
            int maxScore = int.MinValue;
            foreach (var move in availableMoves)
            {
                board[move] = botSymbol;
                int score = Minimax(board, depth + 1, false, botSymbol, opponentSymbol);
                board[move] = CellState.Empty;
                maxScore = Math.Max(score, maxScore);
            }
            return maxScore;
        }
        else
        {
            int minScore = int.MaxValue;
            foreach (var move in availableMoves)
            {
                board[move] = opponentSymbol;
                int score = Minimax(board, depth + 1, true, botSymbol, opponentSymbol);
                board[move] = CellState.Empty;
                minScore = Math.Min(score, minScore);
            }
            return minScore;
        }
    }

    private static List<int> GetAvailableMoves(CellState[] board)
    {
        var moves = new List<int>();
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == CellState.Empty)
            {
                moves.Add(i);
            }
        }
        return moves;
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