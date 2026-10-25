using TicTacToe.Core.Enums;

namespace TicTacToe.Core.Entities
{
    public class GameInvite
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SenderId { get; set; }
        public Guid TargetUserId { get; set; }
        public InviteStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Game? Game { get; set; }
        public User? Sender { get; set; }
        public User? TargetUser { get; set; }
    }
}
