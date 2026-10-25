namespace TicTacToe.Core.Models
{
    public record MoveResult(bool IsSuccess, bool IsGameFinished, string? ErrorMessage = null, Guid ? WinnerId = null)
    {
        public static MoveResult Success(bool IsGameFinished, Guid? WinnerId = null) =>
            new MoveResult(true, IsGameFinished, null, WinnerId);
        public static MoveResult Failure(string ErrorMessage) =>
            new MoveResult(false, false, ErrorMessage);
    }
}
