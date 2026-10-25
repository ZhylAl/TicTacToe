using Microsoft.AspNetCore.Identity;

namespace TicTacToe.Core.Entities
{
    public class User : IdentityUser<Guid>
    {
        public int WinsCount { get; set; }
        public int LossesCount { get; set; }
        public int DrawsCount { get; set; }
    }
}
