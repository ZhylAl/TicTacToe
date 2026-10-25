using TicTacToe.Core.Enums;

namespace TicTacToe.Core.Entities
{
    public class Game
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PlayerXId { get; set; }
        public Guid PlayerOId { get; set; }
        public CellState[] Board { get; set; } = new CellState[9];
        public CellState CurrentTurn { get; set; } = CellState.X;
        public GameStatus Status { get; set; }
        public Guid? WinnerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }
    }
}
