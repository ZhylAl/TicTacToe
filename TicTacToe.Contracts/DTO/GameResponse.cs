namespace TicTacToe.Contracts.DTO
{
    public record GameResponse(
        Guid GameId,
        string Board,
        Guid CurrentPlayerId,
        bool isFinished,
        Guid? WinnerId,
        string Message
        )
    {
    }
}
