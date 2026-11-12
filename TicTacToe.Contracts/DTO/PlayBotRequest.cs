namespace TicTacToe.Contracts.DTO
{
    public record PlayBotRequest(
        Guid PlayerId,
        BotDifficulty Difficulty
        );
}
