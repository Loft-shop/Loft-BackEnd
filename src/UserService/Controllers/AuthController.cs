using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UserService.DTOs;
using UserService.Entities;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { status = "UserService is running", timestamp = DateTime.UtcNow });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _userService.IsEmailTaken(request.Email))
            return BadRequest(new { message = "Email already taken" });

        var name = request.Email.Split('@')[0];

        var userDto = new UserDTO(0, name, request.Email, Role.CUSTOMER, string.Empty, string.Empty, string.Empty, string.Empty, false);

        try
        {
            var created = await _userService.CreateUser(userDto, request.Password);
            var token = await _userService.GenerateJwt(created);
            return Created($"/api/users/{created.Id}", new { user = created, token });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userService.AuthenticateUser(request.Email, request.Password);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials" });

        var token = await _userService.GenerateJwt(user);
        return Ok(new { success = true, message = "Authenticated", user, token });
    }
}
