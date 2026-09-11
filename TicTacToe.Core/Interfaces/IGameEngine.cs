using TicTacToe.Core.Entities;
using TicTacToe.Core.Models;

namespace TicTacToe.Core.Interfaces
{
    public interface IGameEngine
    {
        public MoveResult MakeMove(Game game, Guid playerId, int position);
    }
}
