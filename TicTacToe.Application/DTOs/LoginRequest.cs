using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Application.DTOs
{
    public record LoginRequest(
        [Required][EmailAddress]string Email,
        [Required]string Password
    );
}
