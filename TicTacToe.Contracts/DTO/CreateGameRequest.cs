using TicTacToe.Contracts.Enums;

namespace TicTacToe.Contracts.DTO
{
    public record CreateGameRequest(
        GameMode Mode,
        BotDifficulty? Difficulty = null
    );
}