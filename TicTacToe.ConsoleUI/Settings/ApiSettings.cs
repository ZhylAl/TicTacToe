using System.ComponentModel.DataAnnotations;

namespace TicTacToe.ConsoleUI.Settings
{
    public class ApiSettings
    {
        public const string SectionName = "ApiSettings";

        [Required(ErrorMessage = "BaseUrl is required in appsettings.json!")]
        public string BaseUrl { get; set; } = string.Empty;
    }
}