using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TicTacToe.Contracts.DTO;

namespace TicTacToe.ConsoleUI.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        public string? Token { get; private set; }
        public string? CurrentUsername { get; private set; }

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public void SetToken(string token, string username)
        {
            Token = token;
            CurrentUsername = username;
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
            if (!response.IsSuccessStatusCode)
                return false;

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return false;

            SetToken(auth.Token, auth.Username);
            return true;
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(string email, string username, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", new RegisterRequest(email, username, password));
            if (response.IsSuccessStatusCode)
                return (true, null);

            var errorJson = await response.Content.ReadAsStringAsync();
            return (false, errorJson);
        }

        public async Task<bool> GuestLoginAsync()
        {
            var response = await _http.PostAsync("api/auth/guest", null);
            if (!response.IsSuccessStatusCode)
                return false;

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return false;

            SetToken(auth.Token, auth.Username);
            return true;
        }

        public async Task<GameResponse> CreateGameAsync(CreateGameRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/game/create", request);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<GameResponse>())!;
        }

        public async Task<IReadOnlyList<GameHistoryItemResponse>> GetHistoryAsync()
        {
            var response = await _http.GetFromJsonAsync<List<GameHistoryItemResponse>>("api/game/history");
            return response ?? new List<GameHistoryItemResponse>();
        }
    }
}