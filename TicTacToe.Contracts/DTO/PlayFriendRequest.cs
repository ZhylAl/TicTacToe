namespace TicTacToe.Contracts.DTO
{
    public record PlayFriendRequest(
        Guid PlayerXId,
        Guid PlayerOId
        );
}
