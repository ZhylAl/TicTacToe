using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TicTacToe.Application.Interfaces;
using ContractEnums = TicTacToe.Contracts.Enums;

namespace TicTacToe.Api.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GameHub : Hub
    {
        private readonly IGameService _gameService;

        public GameHub(IGameService gameService)
        {
            _gameService = gameService;
        }

        public async Task WaitForGame(Guid gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
        }

        public async Task JoinGame(Guid gameId)
        {
            try
            {
                var userIdString = Context.UserIdentifier;
                var userId = Guid.Parse(userIdString!);

                var gameResponse = await _gameService.JoinGameAsync(gameId, userId, CancellationToken.None);
                
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
                await Clients.Group(gameId.ToString()).SendAsync("GameStarted", gameResponse);
            }
            catch (Exception ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task MakeMove(Guid gameId, int position)
        {
            try
            {
                var userIdString = Context.UserIdentifier;
                var userId = Guid.Parse(userIdString!);
                
                var gameResponse = await _gameService.MakeMoveAsync(gameId, userId, position, CancellationToken.None);

                await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
                await Clients.Group(gameId.ToString()).SendAsync("GameUpdated", gameResponse);

                if (gameResponse.Status == ContractEnums.GameStatus.InProgress)
                {
                    var botResponse = await _gameService.MakeBotMoveAsync(gameId, CancellationToken.None);
                    if (botResponse != null)
                    {
                        await Task.Delay(400); 
                        await Clients.Group(gameId.ToString()).SendAsync("GameUpdated", botResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new HubException(ex.Message);
            }
        }
    }
}
