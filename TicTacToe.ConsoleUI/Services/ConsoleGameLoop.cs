using Spectre.Console;
using System;
using System.Threading.Tasks;
using TicTacToe.ConsoleUI.UI;
using TicTacToe.Contracts.DTO;
using TicTacToe.Contracts.Enums;

namespace TicTacToe.ConsoleUI.Services
{
    public class ConsoleGameLoop
    {
        private readonly ApiClient _apiClient;
        private readonly GameHubClient _hubClient;
        private TaskCompletionSource<GameResponse>? _turnTcs;
        private GameResponse? _currentGame;

        public ConsoleGameLoop(ApiClient apiClient, GameHubClient hubClient)
        {
            _apiClient = apiClient;
            _hubClient = hubClient;

            _hubClient.OnGameUpdated += (updatedGame) =>
            {
                _currentGame = updatedGame;
                _turnTcs?.TrySetResult(updatedGame);
            };
        }

        public async Task PlayWithFriendAsync()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]Hot-Seat Setup[/]").LeftJustified());

            var playerXName = _apiClient.CurrentUsername ?? "Player X";
            var playerOName = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [bold yellow]Player O[/] nickname:")
                    .DefaultValue("Friend"));

            var game = await _apiClient.CreateGameAsync(new CreateGameRequest(GameMode.HotSeat));

            await _hubClient.WaitForGameAsync(game.Id);

            await RunGameLoopAsync(game, playerXName, playerOName, isVsBot: false);
        }

        public async Task PlayVsBotAsync()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]Bot Match Setup[/]").LeftJustified());

            var difficulty = AnsiConsole.Prompt(
                new SelectionPrompt<BotDifficulty>()
                    .Title("Choose [yellow]Bot Difficulty[/]:")
                    .AddChoices(BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard));

            var playerXName = _apiClient.CurrentUsername ?? "Player";
            var playerOName = $"Bot ({difficulty})";

            var game = await _apiClient.CreateGameAsync(new CreateGameRequest(GameMode.VsBot, difficulty));

            await _hubClient.WaitForGameAsync(game.Id);

            await RunGameLoopAsync(game, playerXName, playerOName, isVsBot: true);
        }

        private async Task RunGameLoopAsync(GameResponse initialGame, string playerXName, string playerOName, bool isVsBot)
        {
            _currentGame = initialGame;

            while (_currentGame.Status == GameStatus.InProgress)
            {
                RenderMatchState(playerXName, playerOName);

                var isXTurn = _currentGame.CurrentTurn == CellState.X;
                var currentName = isXTurn ? $"[bold cyan]{playerXName}[/]" : $"[bold yellow]{playerOName}[/]";
                var currentSymbol = isXTurn ? "[bold cyan]X[/]" : "[bold yellow]O[/]";

                if (isXTurn || !isVsBot)
                {
                    await HandlePlayerTurnAsync(currentName, currentSymbol);
                }
                else
                {
                    await WaitForBotTurnAsync(playerOName);
                }
            }

            RenderGameOver(playerXName, playerOName);
        }

        private void RenderMatchState(string playerXName, string playerOName)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[cyan]Tic-Tac-Toe Match[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[bold cyan]{playerXName} (X)[/]  vs  [bold yellow]{playerOName} (O)[/]\n");

            BoardRenderer.Render(_currentGame!.Board);
            AnsiConsole.WriteLine();
        }

        private async Task HandlePlayerTurnAsync(string currentName, string currentSymbol)
        {
            var cell = AnsiConsole.Prompt(
                new TextPrompt<int>($"Turn: {currentName} ({currentSymbol}). Choose cell [grey](1-9)[/]:")
                    .Validate(num =>
                    {
                        if (num is < 1 or > 9)
                            return ValidationResult.Error("[red]Position must be between 1 and 9.[/]");

                        if (_currentGame!.Board[num - 1] != CellState.Empty)
                            return ValidationResult.Error("[red]Cell is already occupied![/]");

                        return ValidationResult.Success();
                    }));

            _turnTcs = new TaskCompletionSource<GameResponse>();

            await _hubClient.MakeMoveAsync(_currentGame!.Id, cell - 1);

            await _turnTcs.Task;
        }

        private async Task WaitForBotTurnAsync(string botName)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"[yellow]{botName} is thinking...[/]", async _ =>
                {
                    _turnTcs = new TaskCompletionSource<GameResponse>();
                    await _turnTcs.Task;
                });
        }

        private void RenderGameOver(string playerXName, string playerOName)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[cyan]Tic-Tac-Toe Match[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[bold cyan]{playerXName} (X)[/]  vs  [bold yellow]{playerOName} (O)[/]\n");
            BoardRenderer.Render(_currentGame!.Board);
            AnsiConsole.Write(new Rule("[green]Game Over[/]").RuleStyle("green"));
            AnsiConsole.WriteLine();

            if (_currentGame.WinnerId != null)
            {
                var winnerName = _currentGame.WinnerId == _currentGame.PlayerXId ? playerXName : playerOName;
                var winnerColor = _currentGame.WinnerId == _currentGame.PlayerXId ? "cyan" : "yellow";
                AnsiConsole.MarkupLine($"[bold green]🏆 Winner: [{winnerColor}]{winnerName}[/]! Congratulations! 🏆[/]\n");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold yellow]🤝 It's a Draw! Well played both! 🤝[/]\n");
            }

            AnsiConsole.MarkupLine($"[grey]Game ID: {_currentGame.Id}[/]");
            AnsiConsole.MarkupLine("Press [yellow]any key[/] to return to menu...");
            Console.ReadKey(true);
        }
    }
}