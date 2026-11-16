using TicTacToe.Contracts.DTO;

namespace TicTacToe.Application.Interfaces
{
    public interface IGameService
    {
        Task<GameResponse> CreateGameAsync(Guid playerXId, CreateGameRequest request, CancellationToken cancellationToken);
        Task<GameResponse?> GetGameAsync(Guid gameId, CancellationToken cancellationToken);
        Task<GameResponse> JoinGameAsync(Guid gameId, Guid playerOId, CancellationToken cancellationToken);
        Task<GameResponse> MakeMoveAsync(Guid gameId, Guid playerId, int position, CancellationToken cancellationToken);
        Task<GameResponse?> MakeBotMoveAsync(Guid gameId, CancellationToken cancellationToken);
        Task<IReadOnlyList<GameHistoryItemResponse>> GetRecentGamesAsync(int count, CancellationToken cancellationToken);
    }
}
