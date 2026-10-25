using TicTacToe.Core.Enums;

namespace TicTacToe.Application.DTOs
{
    public record GameResponse(
        Guid Id,
        Guid PlayerX,
        Guid PlayerO,
        CellState[] Board,
        CellState CurrentTurn,
        GameStatus Status,
        Guid? WinnerId = null
    );
}
