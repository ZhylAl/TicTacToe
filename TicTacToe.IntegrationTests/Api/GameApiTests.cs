using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Application.DTOs;
using TicTacToe.Core.Enums;
using TicTacToe.Infrastructure.Data;
using TicTacToe.IntegrationTests.Infrastructure;

namespace TicTacToe.IntegrationTests.Api
{
    [Collection("Database Collection")]
    public class GameApiTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly System.Net.Http.HttpClient _client;

        public GameApiTests(DatabaseFixture fixture)
        {
            _factory = new CustomWebApplicationFactory(fixture.GetConnectionString());
            _client = _factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Games\" CASCADE;");
            await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AspNetUsers\" CASCADE;");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private async Task<string> RegisterAndLoginAsync(string username)
        {
            var email = $"{username}{Guid.NewGuid()}@example.com";
            var password = "Password123!";
            
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, username, password));
            registerResponse.EnsureSuccessStatusCode();

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
            loginResponse.EnsureSuccessStatusCode();
            
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            return authResponse!.Token;
        }

        private async Task<HubConnection> CreateHubConnectionAsync(string token)
        {
            var hubUrl = new Uri(_factory.Server.BaseAddress, "gamehub");
            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => 
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult(token)!;
                })
                .Build();
            
            await connection.StartAsync();
            return connection;
        }

        [Fact]
        public async Task FullE2E_RegisterLoginCreateGameAndMakeMove()
        {
            var tokenX = await RegisterAndLoginAsync("PlayerX");
            var tokenO = await RegisterAndLoginAsync("PlayerO");
            
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenX);
            
            var createGameResponse = await _client.PostAsync("/api/game/create", null);
            var game = await createGameResponse.Content.ReadFromJsonAsync<GameResponse>();
            
            var connectionX = await CreateHubConnectionAsync(tokenX);
            var connectionO = await CreateHubConnectionAsync(tokenO);
            
            var gameUpdatedTcs = new TaskCompletionSource<GameResponse>();
            connectionX.On<GameResponse>("GameUpdated", gr => gameUpdatedTcs.TrySetResult(gr));
            
            // O joins the game
            await connectionO.InvokeAsync("JoinGame", game!.Id);
            
            // X makes a move
            await connectionX.InvokeAsync("MakeMove", game.Id, 0); 
            
            var updatedGame = await gameUpdatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));

            Assert.NotNull(updatedGame);
            Assert.Equal(CellState.X, updatedGame.Board[0]);
        }

        [Fact]
        public async Task E2E_PlayerO_TriesToMoveOutOfTurn_ShouldFailAndNotUpdateBoard()
        {
            var tokenX = await RegisterAndLoginAsync("PlayerX");
            var tokenO = await RegisterAndLoginAsync("PlayerO");

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenX);
            var createGameResponse = await _client.PostAsync("/api/game/create", null);
            var game = await createGameResponse.Content.ReadFromJsonAsync<GameResponse>();

            var connectionO = await CreateHubConnectionAsync(tokenO);
            await connectionO.InvokeAsync("JoinGame", game!.Id);

            // O tries to make the first move (which is X's turn)
            var ex = await Assert.ThrowsAsync<HubException>(() => connectionO.InvokeAsync("MakeMove", game.Id, 0));
            Assert.Contains("It's not the player's turn.", ex.Message);

            // Verify board is still empty
            var getGameResponse = await _client.GetAsync($"/api/game/{game.Id}");
            var savedGame = await getGameResponse.Content.ReadFromJsonAsync<GameResponse>();
            Assert.Equal(CellState.Empty, savedGame!.Board[0]);
        }

        [Fact]
        public async Task E2E_GameWin_ShouldUpdateStatusAndSetWinner()
        {
            var tokenX = await RegisterAndLoginAsync("PlayerX");
            var tokenO = await RegisterAndLoginAsync("PlayerO");

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenX);
            var createGameResponse = await _client.PostAsync("/api/game/create", null);
            var game = await createGameResponse.Content.ReadFromJsonAsync<GameResponse>();

            var connectionX = await CreateHubConnectionAsync(tokenX);
            var connectionO = await CreateHubConnectionAsync(tokenO);

            await connectionO.InvokeAsync("JoinGame", game!.Id);

            // Moves to simulate X winning: X:0, O:3, X:1, O:4, X:2
            await connectionX.InvokeAsync("MakeMove", game.Id, 0);
            await connectionO.InvokeAsync("MakeMove", game.Id, 3);
            await connectionX.InvokeAsync("MakeMove", game.Id, 1);
            await connectionO.InvokeAsync("MakeMove", game.Id, 4);
            await connectionX.InvokeAsync("MakeMove", game.Id, 2);

            var getGameResponse = await _client.GetAsync($"/api/game/{game.Id}");
            var savedGame = await getGameResponse.Content.ReadFromJsonAsync<GameResponse>();

            Assert.Equal(GameStatus.Finished, savedGame!.Status);
            Assert.Equal(savedGame.PlayerX, savedGame.WinnerId);
        }

        [Fact]
        public async Task E2E_GameDraw_ShouldUpdateStatusAndNoWinner()
        {
            var tokenX = await RegisterAndLoginAsync("PlayerX");
            var tokenO = await RegisterAndLoginAsync("PlayerO");

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenX);
            var createGameResponse = await _client.PostAsync("/api/game/create", null);
            var game = await createGameResponse.Content.ReadFromJsonAsync<GameResponse>();

            var connectionX = await CreateHubConnectionAsync(tokenX);
            var connectionO = await CreateHubConnectionAsync(tokenO);

            await connectionO.InvokeAsync("JoinGame", game!.Id);

            /*
               X O X
               X O O
               O X X
            */
            // X:0, O:1, X:2,
            // O:4, X:3, O:5, 
            // X:7, O:6, X:8

            await connectionX.InvokeAsync("MakeMove", game.Id, 0);
            await connectionO.InvokeAsync("MakeMove", game.Id, 1);
            await connectionX.InvokeAsync("MakeMove", game.Id, 2);
            await connectionO.InvokeAsync("MakeMove", game.Id, 4);
            await connectionX.InvokeAsync("MakeMove", game.Id, 3);
            await connectionO.InvokeAsync("MakeMove", game.Id, 5);
            await connectionX.InvokeAsync("MakeMove", game.Id, 7);
            await connectionO.InvokeAsync("MakeMove", game.Id, 6);
            await connectionX.InvokeAsync("MakeMove", game.Id, 8);

            var getGameResponse = await _client.GetAsync($"/api/game/{game.Id}");
            var savedGame = await getGameResponse.Content.ReadFromJsonAsync<GameResponse>();

            Assert.Equal(GameStatus.Finished, savedGame!.Status);
            Assert.Null(savedGame.WinnerId);
        }
    }
}
