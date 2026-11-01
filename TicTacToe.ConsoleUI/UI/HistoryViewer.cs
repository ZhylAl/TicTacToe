using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Application.Interfaces;

namespace TicTacToe.ConsoleUI.UI;

public class HistoryViewer
{
    private readonly IGameService _gameService;

    public HistoryViewer(IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task ShowAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Recent Games History[/]").RuleStyle("yellow"));
        AnsiConsole.WriteLine();

        var games = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Loading history from database...", async _ =>
                await _gameService.GetRecentGamesAsync(10, cancellationToken)); // Fetch the 10 most recent games

        if (games.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No games found in history yet. Play some matches first![/]");
        }
        else
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey);

            table.AddColumn(new TableColumn("[bold]#[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Date (UTC)[/]").Centered());
            table.AddColumn(new TableColumn("[bold cyan]Player X[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold yellow]Player O[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
            table.AddColumn(new TableColumn("[bold green]Result[/]").Centered());

            int index = 1;
            foreach (var game in games)
            {
                var resultFormatted = game.Result switch
                {
                    "Draw" => "[bold yellow]Draw[/]",
                    var r when r != null && r.EndsWith("won") => $"[bold green]{r}[/]",
                    _ => $"[grey]{game.Result}[/]"
                };

                table.AddRow(
                    index.ToString(),
                    game.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    $"[cyan]{game.PlayerXName}[/]",
                    $"[yellow]{game.PlayerOName}[/]",
                    $"[grey]{game.Status}[/]",
                    resultFormatted
                );
                index++;
            }

            AnsiConsole.Write(new Align(table, HorizontalAlignment.Center));
        }

        AnsiConsole.MarkupLine("\nPress [yellow]any key[/] to return to menu...");
        Console.ReadKey(true);
    }
}