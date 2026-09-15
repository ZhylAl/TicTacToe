using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Infrastructure.Settings
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        [Required]
        public string Key { get; set; }
    }
}
