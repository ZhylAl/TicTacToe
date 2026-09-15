using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Application.DTOs;
using TicTacToe.Application.Interfaces;
using TicTacToe.Core.Entities;          

namespace TicTacToe.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IJwtProvider _jwtProvider;

        public AuthController(UserManager<User> userManager, IJwtProvider jwtProvider)
        {
            _userManager = userManager;
            _jwtProvider = jwtProvider;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            var user = new User
            {
                Email = registerRequest.Email,
                UserName = registerRequest.Username
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);
              
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
           var user = await _userManager.FindByEmailAsync(loginRequest.Email);
           if (user == null)
            return Unauthorized();

           var passwordValid = await _userManager.CheckPasswordAsync(user, loginRequest.Password);
           if (!passwordValid)
               return Unauthorized();

           var token = _jwtProvider.GenerateToken(user);
           return Ok(new AuthResponse(token, user.UserName!));
        }
    }
}
