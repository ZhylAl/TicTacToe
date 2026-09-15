using TicTacToe.Application.DTOs;

namespace TicTacToe.Application.Interfaces
{
    public interface IGameService
    {
        Task<GameResponse> CreateGameAsync(Guid playerXId, CancellationToken cancellationToken);
        Task<GameResponse?> GetGameAsync(Guid gameId, CancellationToken cancellationToken);
        Task<GameResponse> JoinGameAsync(Guid gameId, Guid playerOId, CancellationToken cancellationToken);
        Task<GameResponse> MakeMoveAsync(Guid gameId, Guid playerId, int position, CancellationToken cancellationToken);
    }
}
