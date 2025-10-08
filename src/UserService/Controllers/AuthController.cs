using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Loft.Common.DTOs;
using UserService.Services;
using UserService.Entities;
using UserService.DTOs;

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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _userService.IsEmailTaken(request.Email))
            return BadRequest(new { message = "Email already taken" });

        // For self-registration: role is always CUSTOMER; selling ability is controlled by CanSell flag
        var userDto = new UserDTO(0, request.Name ?? string.Empty, request.Email, Loft.Common.Enums.Role.CUSTOMER.ToString(), string.Empty, string.Empty, string.Empty, string.Empty, false);

        try
        {
            var created = await _userService.CreateUser(userDto, request.Password);
            // generate token
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
