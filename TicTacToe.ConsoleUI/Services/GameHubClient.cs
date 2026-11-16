using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using TicTacToe.Contracts.DTO;

namespace TicTacToe.ConsoleUI.Services
{
    public class GameHubClient : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly string _hubUrl;

        public event Action<GameResponse>? OnGameStarted;
        public event Action<GameResponse>? OnGameUpdated;

        public GameHubClient(IConfiguration configuration)
        {
            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5146";
            _hubUrl = $"{baseUrl.TrimEnd('/')}/gamehub";
        }

        public async Task ConnectAsync(string token)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<GameResponse>("GameStarted", game =>
            {
                OnGameStarted?.Invoke(game);
            });

            _hubConnection.On<GameResponse>("GameUpdated", game =>
            {
                OnGameUpdated?.Invoke(game);
            });

            await _hubConnection.StartAsync();
        }

        public async Task WaitForGameAsync(Guid gameId)
        {
            EnsureConnected();
            await _hubConnection!.InvokeAsync("WaitForGame", gameId);
        }

        public async Task JoinGameAsync(Guid gameId)
        {
            EnsureConnected();
            await _hubConnection!.InvokeAsync("JoinGame", gameId);
        }

        public async Task MakeMoveAsync(Guid gameId, int position)
        {
            EnsureConnected();
            await _hubConnection!.InvokeAsync("MakeMove", gameId, position);
        }

        private void EnsureConnected()
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("SignalR connection is not established. Call ConnectAsync first.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}