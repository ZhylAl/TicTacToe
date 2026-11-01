using TicTacToe.Core.Enums;

namespace TicTacToe.Core.Interfaces;

public interface IBotPlayer
{
    int GetMove(CellState[] board, CellState botSymbol, BotDifficulty difficulty);
}