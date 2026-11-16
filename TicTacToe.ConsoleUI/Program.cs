using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using TicTacToe.ConsoleUI.Services;
using TicTacToe.ConsoleUI.UI;

namespace TicTacToe.ConsoleUI
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            var builder = Host.CreateApplicationBuilder(args);

            var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5146";
            builder.Services.AddHttpClient("GameApi", client =>
            {
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
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

            var host = builder.Build();

            var apiClient = host.Services.GetRequiredService<ApiClient>();
            var hubClient = host.Services.GetRequiredService<GameHubClient>();
            var gameLoop = host.Services.GetRequiredService<ConsoleGameLoop>();
            var historyViewer = host.Services.GetRequiredService<HistoryViewer>();

            await AuthenticateUserAsync(apiClient);

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Connecting to game server...", async _ =>
                {
                    await hubClient.ConnectAsync(apiClient.Token!);
                });

            AnsiConsole.MarkupLine($"[green]✔ Connected as [bold yellow]{apiClient.CurrentUsername}[/]![/]\n");
            await Task.Delay(1000);

            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(
                    new FigletText("Tic-Tac-Toe")
                        .Centered()
                        .Color(Color.Cyan1));

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
        }

        private static async Task AuthenticateUserAsync(ApiClient apiClient)
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(
                    new FigletText("Tic-Tac-Toe")
                        .Centered()
                        .Color(Color.Cyan1));

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

                if (choice == "⚡ Play as Guest (No Registration)")
                {
                    var success = await AnsiConsole.Status()
                        .StartAsync("Creating guest session...", async _ => await apiClient.GuestLoginAsync());

                    if (success) return;

                    AnsiConsole.MarkupLine("[red]Failed to connect to API server. Is it running?[/]");
                    Console.ReadKey(true);
                    continue;
                }

                if (choice == "🔑 Login")
                {
                    var email = AnsiConsole.Ask<string>("Enter your [yellow]Email[/]:");
                    var password = AnsiConsole.Prompt(new TextPrompt<string>("Enter your [yellow]Password[/]:").Secret());

                    var success = await AnsiConsole.Status()
                        .StartAsync("Logging in...", async _ => await apiClient.LoginAsync(email, password));

                    if (success) return;

                    AnsiConsole.MarkupLine("[red]Invalid email or password![/]");
                    Console.ReadKey(true);
                    continue;
                }

                if (choice == "📝 Register")
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
                        continue;
                    }

                    await apiClient.LoginAsync(email, password);
                    return;
                }
            }
        }
    }
}