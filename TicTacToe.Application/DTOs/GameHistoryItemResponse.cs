namespace TicTacToe.Application.DTOs;

public record GameHistoryItemResponse(
    Guid Id,
    string PlayerXName,
    string? PlayerOName,
    string Status,
    string? Result,
    DateTime CreatedAt
);