using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System;
using System.Threading.Tasks;
using TicTacToe.Application;
using TicTacToe.ConsoleUI.Services;
using TicTacToe.ConsoleUI.UI;
using TicTacToe.Infrastructure;
using TicTacToe.Infrastructure.Data;

namespace TicTacToe.ConsoleUI;

internal class Program
{
    static async Task Main(string[] args)
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);

        builder.Services.AddScoped<PlayerManager>();
        builder.Services.AddScoped<ConsoleGameLoop>();
        builder.Services.AddScoped<HistoryViewer>();

        var host = builder.Build();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Connecting to database and applying migrations...", ctx =>
            {
                try
                {
                    using var scope = host.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine("[bold red]Failed to connect to PostgreSQL![/]");
                    AnsiConsole.MarkupLine("[yellow]Please make sure your database is running (e.g. 'docker compose up -d').[/]");
                    AnsiConsole.MarkupLine($"[grey]Error details: {ex.Message}[/]\n");
                    Environment.Exit(1);
                }
            });

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new FigletText("Tic-Tac-Toe")
                    .Centered()
                    .Color(Color.Cyan1));
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose an option:")
                    .AddChoices(new[] {
                        "👥 Play with Friend",
                        "🤖 Play vs Bot",
                        "📜 Game History",
                        "🚪 Exit"
                    }));

            if (choice == "🚪 Exit")
            {
                AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                break;
            }
            using (var scope = host.Services.CreateScope())
            {
                switch (choice)
                {
                    case "👥 Play with Friend":
                        var friendGame = scope.ServiceProvider.GetRequiredService<ConsoleGameLoop>();
                        await friendGame.PlayWithFriendAsync();
                        break;

                    case "🤖 Play vs Bot":
                        var botGame = scope.ServiceProvider.GetRequiredService<ConsoleGameLoop>();
                        await botGame.PlayVsBotAsync();
                        break;

                    case "📜 Game History":
                        var historyViewer = scope.ServiceProvider.GetRequiredService<HistoryViewer>();
                        await historyViewer.ShowAsync();
                        break;
                }
            }
        }
    }
}