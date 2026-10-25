using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Application.DTOs
{
    public record RegisterRequest(
        [Required][EmailAddress]string Email,
        [Required]string Username,
        [Required]string Password
    );
}
