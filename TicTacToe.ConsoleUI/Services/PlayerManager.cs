using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using TicTacToe.Core.Entities;

namespace TicTacToe.ConsoleUI.Services;

public class PlayerManager
{
    private readonly UserManager<User> _userManager;

    public PlayerManager(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<User> GetOrCreatePlayerAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user != null)
        {
            return user;
        }

        user = new User
        {
            UserName = username
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user '{username}': {errors}");
        }

        return user;
    }
}