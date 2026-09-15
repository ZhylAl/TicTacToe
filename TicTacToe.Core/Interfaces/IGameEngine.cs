using TicTacToe.Core.Entities;
using TicTacToe.Core.Models;

namespace TicTacToe.Core.Interfaces
{
    public interface IGameEngine
    {
        MoveResult MakeMove(Game game, Guid playerId, int position);
    }
}
