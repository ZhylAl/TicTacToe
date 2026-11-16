namespace TicTacToe.Contracts.DTO
{
    public record GameHistoryItemResponse(
        Guid Id,
        string PlayerXName,
        string? PlayerOName,
        string Status,
        string? Result,
        DateTime CreatedAt
    );
}
