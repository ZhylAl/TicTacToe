using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;
using TicTacToe.Application.Interfaces;
using TicTacToe.ConsoleUI.UI;
using TicTacToe.Core.Entities;
using TicTacToe.Core.Enums;
using TicTacToe.Core.Interfaces;

namespace TicTacToe.ConsoleUI.Services;

public class ConsoleGameLoop
{
    private readonly IGameService _gameService;
    private readonly PlayerManager _playerManager;
    private readonly IBotPlayer _botPlayer;

    public ConsoleGameLoop(
        IGameService gameService,
        PlayerManager playerManager,
        IBotPlayer botPlayer)
    {
        _gameService = gameService;
        _playerManager = playerManager;
        _botPlayer = botPlayer;
    }

    public Task PlayWithFriendAsync(CancellationToken cancellationToken = default)
    => RunGameAsync(vsBot: false, cancellationToken);
    public Task PlayVsBotAsync(CancellationToken cancellationToken = default)
        => RunGameAsync(vsBot: true, cancellationToken);

    private async Task RunGameAsync(bool vsBot, CancellationToken cancellationToken = default)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Game Setup[/]").LeftJustified());

        // 1. Setup Player X (Human)
        AnsiConsole.MarkupLine("[bold cyan]Player X setup:[/]");
        var playerX = await PromptPlayerAsync("Player X");
        AnsiConsole.WriteLine();

        // 2. Setup Player O (Human or Bot)
        User playerO;
        BotDifficulty difficulty = BotDifficulty.Easy;

        if (vsBot)
        {
            difficulty = AnsiConsole.Prompt(
                new SelectionPrompt<BotDifficulty>()
                    .Title("Choose [yellow]Bot Difficulty[/]:")
                    .AddChoices(BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard));

            var botName = $"Bot{difficulty}";
            playerO = await _playerManager.GetOrCreatePlayerAsync(botName);
            AnsiConsole.MarkupLine($"Playing against [bold yellow]{playerO.UserName}[/]!");
        }
        else
        {
            AnsiConsole.MarkupLine("[bold yellow]Player O setup:[/]");
            playerO = await PromptPlayerAsync("Player O", forbiddenName: playerX.UserName);
        }

        AnsiConsole.MarkupLine("\n[grey]Creating game in database...[/]");

        // 3. Create game and join Player O
        var game = await _gameService.CreateGameAsync(playerX.Id, cancellationToken);
        game = await _gameService.JoinGameAsync(game.Id, playerO.Id, cancellationToken);

        // 4. Main turn loop
        while (game.Status == GameStatus.InProgress)
        {
            AnsiConsole.Clear();

            // Header
            AnsiConsole.Write(new Rule("[cyan]Tic-Tac-Toe Match[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine(
                $"[bold cyan]{playerX.UserName} (X)[/]  vs  [bold yellow]{playerO.UserName} (O)[/]\n");

            // Board
            BoardRenderer.Render(game.Board);
            AnsiConsole.WriteLine();

            var isXTurn = game.CurrentTurn == CellState.X;
            var currentUserId = isXTurn ? playerX.Id : playerO.Id;
            var currentUserName = isXTurn ? $"[bold cyan]{playerX.UserName}[/]" : $"[bold yellow]{playerO.UserName}[/]";
            var currentSymbol = isXTurn ? "[bold cyan]X[/]" : "[bold yellow]O[/]";

            int boardPosition;

            // Humans turn
            if (isXTurn || !vsBot)
            {
                var positionNumber = AnsiConsole.Prompt(
                    new TextPrompt<int>($"Turn: {currentUserName} ({currentSymbol}). Choose cell [grey](1-9)[/]:")
                            .Validate(num =>
                        {
                            if (num is < 1 or > 9)
                                return ValidationResult.Error("[red]Position must be between 1 and 9.[/]");

                            if (game.Board[num - 1] != CellState.Empty)
                                return ValidationResult.Error("[red]Cell is already occupied! Choose another.[/]");

                            return ValidationResult.Success();
                        }));

                boardPosition = positionNumber - 1;
            }
            else
            {
                // Bot
                AnsiConsole.MarkupLine($"[grey]{playerO.UserName} is thinking...[/]");
                await Task.Delay(500, cancellationToken); // slight delay for natural feeling

                boardPosition = _botPlayer.GetMove(game.Board, CellState.O, difficulty);
            }

            // Apply move
            game = await _gameService.MakeMoveAsync(game.Id, currentUserId, boardPosition, cancellationToken);
        }

        // 5. Game finished
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[cyan]Tic-Tac-Toe Match[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine(
            $"[bold cyan]{playerX.UserName} (X)[/]  vs  [bold yellow]{playerO.UserName} (O)[/]\n");
        BoardRenderer.Render(game.Board);
        AnsiConsole.Write(new Rule("[green]Game Over[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        if (game.WinnerId != null)
        {
            var winnerName = game.WinnerId == playerX.Id ? playerX.UserName : playerO.UserName;
            var winnerColor = game.WinnerId == playerX.Id ? "cyan" : "yellow";
            AnsiConsole.MarkupLine($"[bold green]🏆 Winner: [{winnerColor}]{winnerName}[/]! Congratulations! 🏆[/]\n");
        }
        else
        {
            AnsiConsole.MarkupLine("[bold yellow]🤝 It's a Draw! Well played both! 🤝[/]\n");
        }

        AnsiConsole.MarkupLine($"[grey]Saved to database. Game ID: {game.Id}[/]");
        AnsiConsole.MarkupLine("Press [yellow]any key[/] to return to menu...");
        Console.ReadKey(true);
    }

    private async Task<User> PromptPlayerAsync(string playerLabel, string? forbiddenName = null)
    {
        var username = AnsiConsole.Prompt(
            new TextPrompt<string>($"[bold yellow]{playerLabel}[/] Enter nickname:")
                .Validate(name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return ValidationResult.Error("[red]Nickname cannot be empty.[/]");

                    if (forbiddenName != null && name.Equals(forbiddenName, StringComparison.OrdinalIgnoreCase))
                        return ValidationResult.Error("[red]Player O must have a different nickname than Player X![/]");

                    return ValidationResult.Success();
                }));

        return await _playerManager.GetOrCreatePlayerAsync(username);
    }
}