namespace TicTacToe.Core.Enums;

public enum BotDifficulty
{
    Easy = 0,       // Random moves
    Medium = 1,     // Smart heuristic: takes winning moves, blocks opponent wins
    Hard = 2        // Unbeatable Minimax algorithm
}