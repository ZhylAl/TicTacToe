using TicTacToe.Core.Entities;

namespace TicTacToe.Application.Interfaces
{
    public interface IJwtProvider
    {
        string GenerateToken(User user);
    }
}
