using TicTacToe.Contracts.Enums;

namespace TicTacToe.Contracts.DTO
{
    public record GameResponse(
        Guid Id,
        Guid PlayerXId,
        Guid? PlayerOId,
        CellState[] Board,
        CellState CurrentTurn,
        GameStatus Status,
        Guid? WinnerId = null
    );
}