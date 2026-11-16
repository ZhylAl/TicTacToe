using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using TicTacToe.Contracts.DTO;
using TicTacToe.Application.Interfaces;
using TicTacToe.Contracts.Enums;
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
            
            var createGameResponse = await _client.PostAsJsonAsync("/api/game/create", new CreateGameRequest(GameMode.PvpOnline));
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
            var createGameResponse = await _client.PostAsJsonAsync("/api/game/create", new CreateGameRequest(GameMode.PvpOnline));
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
            var createGameResponse = await _client.PostAsJsonAsync("/api/game/create", new CreateGameRequest(GameMode.PvpOnline));
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
            Assert.Equal(savedGame.PlayerXId, savedGame.WinnerId);
        }

        [Fact]
        public async Task E2E_GameDraw_ShouldUpdateStatusAndNoWinner()
        {
            var tokenX = await RegisterAndLoginAsync("PlayerX");
            var tokenO = await RegisterAndLoginAsync("PlayerO");

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenX);
            var createGameResponse = await _client.PostAsJsonAsync("/api/game/create", new CreateGameRequest(GameMode.PvpOnline));
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

        [Fact]
        public async Task GetRecentGamesAsync_ShouldReturnGamesWithCorrectPlayerNames()
        {
            // Arrange: Register two players in the database
            var playerXName = $"PlayerX_{Guid.NewGuid():N}";
            var playerOName = $"PlayerO_{Guid.NewGuid():N}";

            var tokenX = await RegisterAndLoginAsync(playerXName);
            var tokenO = await RegisterAndLoginAsync(playerOName);

            // Create game via API
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenX);

            var createResponse = await _client.PostAsJsonAsync("/api/game/create", new CreateGameRequest(GameMode.PvpOnline));
            var createdGame = await createResponse.Content.ReadFromJsonAsync<GameResponse>();

            // Player O joins via SignalR hub
            var connectionO = await CreateHubConnectionAsync(tokenO);
            await connectionO.InvokeAsync("JoinGame", createdGame!.Id);

            // Act: Request recent games history from IGameService
            using var scope = _factory.Services.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            var recentGames = await gameService.GetRecentGamesAsync(10, CancellationToken.None);

            // Assert: Game must be present with resolved player usernames
            Assert.NotEmpty(recentGames);

            var gameInHistory = recentGames.FirstOrDefault(g => g.Id == createdGame.Id);
            Assert.NotNull(gameInHistory);
            Assert.Equal(playerXName, gameInHistory.PlayerXName);
            Assert.Equal(playerOName, gameInHistory.PlayerOName);
            Assert.Equal(GameStatus.InProgress.ToString(), gameInHistory.Status);
        }

        [Fact]
        public async Task GuestLogin_ShouldReturnToken_AndAllowCreatingGame()
        {
            // Act: request a guest token without prior registration
            var guestResponse = await _client.PostAsync("/api/auth/guest", null);
            guestResponse.EnsureSuccessStatusCode();

            var auth = await guestResponse.Content.ReadFromJsonAsync<AuthResponse>();

            // Assert: token is generated and username starts with Guest_
            Assert.NotNull(auth);
            Assert.False(string.IsNullOrEmpty(auth.Token));
            Assert.StartsWith("Guest_", auth.Username);

            // Verify the guest token grants access to protected endpoints
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);

            var createResponse = await _client.PostAsJsonAsync("/api/game/create", new CreateGameRequest(GameMode.HotSeat));
            createResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task CreateGame_HotSeat_ShouldStartImmediatelyWithoutWaiting()
        {
            // Arrange
            var token = await RegisterAndLoginAsync("HotSeatHost");
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act: create a HotSeat game
            var createResponse = await _client.PostAsJsonAsync("/api/game/create", new CreateGameRequest(GameMode.HotSeat));
            var game = await createResponse.Content.ReadFromJsonAsync<GameResponse>();

            // Assert: game starts immediately with InProgress status and assigned PlayerOId
            Assert.NotNull(game);
            Assert.Equal(GameStatus.InProgress, game.Status);
            Assert.NotNull(game.PlayerOId);
        }

        [Fact]
        public async Task VsBot_PlayerMakesMove_BotShouldMakeResponseMoveViaSignalR()
        {
            // Arrange: login and create a game vs Bot
            var token = await RegisterAndLoginAsync("BotChallenger");
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createResponse = await _client.PostAsJsonAsync("/api/game/create",
                new CreateGameRequest(GameMode.VsBot, BotDifficulty.Easy));
            var game = await createResponse.Content.ReadFromJsonAsync<GameResponse>();

            var connection = await CreateHubConnectionAsync(token);
            await connection.InvokeAsync("WaitForGame", game!.Id);

            // Track events: 1st update = human move, 2nd update = bot counter-move
            var moveCount = 0;
            var botMoveTcs = new TaskCompletionSource<GameResponse>();

            connection.On<GameResponse>("GameUpdated", updatedGame =>
            {
                moveCount++;
                if (moveCount == 2)
                {
                    botMoveTcs.TrySetResult(updatedGame);
                }
            });

            // Act: Player X makes a move at cell 0
            await connection.InvokeAsync("MakeMove", game.Id, 0);

            // Wait up to 4 seconds for the bot counter-move event
            var finalBoardState = await botMoveTcs.Task.WaitAsync(TimeSpan.FromSeconds(4));

            // Assert: board must have 2 occupied cells (X and O), turn switches back to X
            Assert.NotNull(finalBoardState);
            Assert.Equal(CellState.X, finalBoardState.Board[0]);
            Assert.Contains(finalBoardState.Board, cell => cell == CellState.O);
            Assert.Equal(CellState.X, finalBoardState.CurrentTurn);
        }
    }
}
