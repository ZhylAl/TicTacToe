using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using TicTacToe.ConsoleUI.Services;
using TicTacToe.ConsoleUI.Settings;
using TicTacToe.ConsoleUI.UI;

namespace TicTacToe.ConsoleUI
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            var builder = Host.CreateApplicationBuilder(args);
            ConfigureServices(builder);
            var host = builder.Build();

            var apiClient = host.Services.GetRequiredService<ApiClient>();
            var hubClient = host.Services.GetRequiredService<GameHubClient>();
            var gameLoop = host.Services.GetRequiredService<ConsoleGameLoop>();
            var historyViewer = host.Services.GetRequiredService<HistoryViewer>();

            await AuthenticateUserAsync(apiClient);

            var isConnected = await ConnectToHubAsync(hubClient, apiClient.Token!);
            if (!isConnected) return;

            AnsiConsole.MarkupLine($"[green]✔ Connected as [bold yellow]{apiClient.CurrentUsername}[/]![/]\n");
            await Task.Delay(1000);

            await RunMainMenuAsync(apiClient, gameLoop, historyViewer);
        }

        private static void ConfigureServices(HostApplicationBuilder builder)
        {
            builder.Services.AddOptions<ApiSettings>()
                .BindConfiguration(ApiSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddHttpClient("GameApi", (serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            });

            builder.Services.AddSingleton<ApiClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("GameApi");
                return new ApiClient(httpClient);
            });

            builder.Services.AddSingleton<GameHubClient>();
            builder.Services.AddSingleton<ConsoleGameLoop>();
            builder.Services.AddTransient<HistoryViewer>();
        }

        private static async Task<bool> ConnectToHubAsync(GameHubClient hubClient, string token)
        {
            try
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Connecting to game server...", async _ =>
                    {
                        await hubClient.ConnectAsync(token);
                    });

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorPanel("SignalR Connection Error",
                    $"Failed to connect to game server: {ex.Message}\nPlease ensure TicTacToe.Api is running.");
                return false;
            }
        }

        private static async Task RunMainMenuAsync(ApiClient apiClient, ConsoleGameLoop gameLoop, HistoryViewer historyViewer)
        {
            while (true)
            {
                RenderHeader();
                AnsiConsole.MarkupLine($"[grey]Logged in as:[/] [bold yellow]{apiClient.CurrentUsername}[/]\n");

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Choose an option:")
                        .AddChoices(new[] {
                            "👥 Play with Friend (Hot-Seat)",
                            "🤖 Play vs Bot",
                            "📜 Game History",
                            "🚪 Exit"
                        }));

                if (choice == "🚪 Exit")
                {
                    AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                    break;
                }

                await ExecuteMenuChoiceAsync(choice, gameLoop, historyViewer);
            }
        }

        private static async Task ExecuteMenuChoiceAsync(string choice, ConsoleGameLoop gameLoop, HistoryViewer historyViewer)
        {
            try
            {
                switch (choice)
                {
                    case "👥 Play with Friend (Hot-Seat)":
                        await gameLoop.PlayWithFriendAsync();
                        break;

                    case "🤖 Play vs Bot":
                        await gameLoop.PlayVsBotAsync();
                        break;

                    case "📜 Game History":
                        await historyViewer.ShowAsync();
                        break;
                }
            }
            catch (HttpRequestException ex)
            {
                var message = ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.InternalServerError =>
                        "The game server encountered an internal error (500). Please check the API/Database logs.",
                    System.Net.HttpStatusCode.Unauthorized =>
                        "Your session has expired. Please restart the app and login again.",
                    _ =>
                        $"Failed to communicate with API server: {ex.Message}"
                };

                ShowErrorPanel("API Connection Error", message);
            }
            catch (Microsoft.AspNetCore.SignalR.HubException ex)
            {
                ShowErrorPanel("Game Server Error", $"Game session error: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowErrorPanel("Unexpected Error", $"An unexpected error occurred: {ex.Message}");
            }
        }

        private static async Task AuthenticateUserAsync(ApiClient apiClient)
        {
            while (true)
            {
                RenderHeader();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Welcome! Please choose an option:")
                        .AddChoices(new[] {
                            "⚡ Play as Guest (No Registration)",
                            "🔑 Login",
                            "📝 Register",
                            "🚪 Exit"
                        }));

                if (choice == "🚪 Exit")
                {
                    Environment.Exit(0);
                }

                var authenticated = choice switch
                {
                    "⚡ Play as Guest (No Registration)" => await HandleGuestLoginAsync(apiClient),
                    "🔑 Login" => await HandleLoginAsync(apiClient),
                    "📝 Register" => await HandleRegisterAsync(apiClient),
                    _ => false
                };

                if (authenticated)
                {
                    break;
                }
            }
        }

        private static async Task<bool> HandleGuestLoginAsync(ApiClient apiClient)
        {
            var success = await AnsiConsole.Status()
                .StartAsync("Creating guest session...", async _ => await apiClient.GuestLoginAsync());

            if (!success)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to API server. Is it running?[/]");
                Console.ReadKey(true);
                return false;
            }

            return true;
        }

        private static async Task<bool> HandleLoginAsync(ApiClient apiClient)
        {
            var email = AnsiConsole.Ask<string>("Enter your [yellow]Email[/]:");
            var password = AnsiConsole.Prompt(new TextPrompt<string>("Enter your [yellow]Password[/]:").Secret());

            var success = await AnsiConsole.Status()
                .StartAsync("Logging in...", async _ => await apiClient.LoginAsync(email, password));

            if (!success)
            {
                AnsiConsole.MarkupLine("[red]Invalid email or password![/]");
                Console.ReadKey(true);
                return false;
            }

            return true;
        }

        private static async Task<bool> HandleRegisterAsync(ApiClient apiClient)
        {
            var username = AnsiConsole.Ask<string>("Choose [yellow]Username[/]:");
            var email = AnsiConsole.Ask<string>("Enter [yellow]Email[/] (e.g. user@test.com):");
            var password = AnsiConsole.Prompt(new TextPrompt<string>("Choose [yellow]Password[/]:").Secret());

            (bool registered, string? error) = (false, null);

            await AnsiConsole.Status()
                .StartAsync("Registering...", async _ =>
                {
                    (registered, error) = await apiClient.RegisterAsync(email, username, password);
                });

            if (!registered)
            {
                AnsiConsole.MarkupLine($"[red]Registration failed:[/] [grey]{Markup.Escape(error ?? "")}[/]");
                Console.ReadKey(true);
                return false;
            }

            await apiClient.LoginAsync(email, password);
            return true;
        }

        private static void RenderHeader()
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new FigletText("Tic-Tac-Toe")
                    .Centered()
                    .Color(Color.Cyan1));
        }

        private static void ShowErrorPanel(string title, string details)
        {
            var panel = new Panel(new Markup($"[red]{Markup.Escape(details)}[/]"))
                .Header($"[bold red] ⚠ {title} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red);

            AnsiConsole.WriteLine();
            AnsiConsole.Write(panel);
            AnsiConsole.MarkupLine("\n[grey]Press any key to return to menu...[/]");
            Console.ReadKey(true);
        }
    }
}