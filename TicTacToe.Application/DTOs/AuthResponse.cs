namespace TicTacToe.Application.DTOs
{
    public record AuthResponse(
        string Token,
        string Username
    );
}
