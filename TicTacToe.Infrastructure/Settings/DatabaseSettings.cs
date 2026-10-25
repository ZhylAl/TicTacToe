using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Infrastructure.Settings
{
    public class DatabaseSettings
    {
        public const string SectionName = "ConnectionStrings";

        [Required]
        public string DefaultConnection { get; set; } = string.Empty;
    }
}
