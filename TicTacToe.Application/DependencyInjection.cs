using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Core.Interfaces;
using TicTacToe.Core.Services;

namespace TicTacToe.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IGameEngine, GameEngine>();

            return services;
        }
    }
}
