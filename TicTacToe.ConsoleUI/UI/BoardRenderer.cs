using Spectre.Console;
using TicTacToe.Core.Enums;

namespace TicTacToe.ConsoleUI.UI;

public static class BoardRenderer
{
    public static void Render(CellState[] board)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .HideHeaders();

        table.AddColumn(new TableColumn("").Centered().Width(5));
        table.AddColumn(new TableColumn("").Centered().Width(5));
        table.AddColumn(new TableColumn("").Centered().Width(5));

        table.AddRow(FormatCell(board[0], 1), FormatCell(board[1], 2), FormatCell(board[2], 3));
        table.AddRow(FormatCell(board[3], 4), FormatCell(board[4], 5), FormatCell(board[5], 6));
        table.AddRow(FormatCell(board[6], 7), FormatCell(board[7], 8), FormatCell(board[8], 9));

        AnsiConsole.Write(new Align(table, HorizontalAlignment.Center));
    }

    private static string FormatCell(CellState state, int positionNumber)
    {
        return state switch
        {
            CellState.X => "[bold cyan]X[/]",
            CellState.O => "[bold yellow]O[/]",
            _ => $"[grey]{positionNumber}[/]"
        };
    }
}