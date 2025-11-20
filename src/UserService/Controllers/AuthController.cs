using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using UserService.DTOs;
using UserService.Entities;
using UserService.Services;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public AuthController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
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

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            
            Console.WriteLine($"[GoogleAuth] Backend expected Client ID: {googleClientId ?? "NULL (NOT SET!)"}");
            
            if (string.IsNullOrEmpty(googleClientId))
            {
                Console.WriteLine("[GoogleAuth] ERROR: Google Client ID is not configured!");
                return Unauthorized(new { message = "Google OAuth not configured on server" });
            }
            
            // Декодируем токен для логирования (без валидации)
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(request.IdToken) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                var tokenAud = jsonToken?.Audiences?.FirstOrDefault();
                Console.WriteLine($"[GoogleAuth] Token's aud (Client ID from frontend): {tokenAud}");
                Console.WriteLine($"[GoogleAuth] Match: {tokenAud == googleClientId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleAuth] Could not decode token for logging: {ex.Message}");
            }
            
            // Verify Google ID token
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            });

            if (payload == null)
            {
                Console.WriteLine("[GoogleAuth] Token validation returned null payload");
                return Unauthorized(new { message = "Invalid Google token" });
            }
            
            Console.WriteLine($"[GoogleAuth] Token validated successfully for email: {payload.Email}");

            // Extract user information
            var email = payload.Email;
            var googleId = payload.Subject;
            var firstName = payload.GivenName;
            var lastName = payload.FamilyName;
            var avatarUrl = payload.Picture;

            // Create or update user
            var user = await _userService.CreateOrUpdateOAuthUser(
                email, 
                "Google", 
                googleId, 
                firstName, 
                lastName, 
                avatarUrl
            );

            Console.WriteLine($"[GoogleAuth] User created/updated: ID={user.Id}, Email={user.Email}");

            // Generate JWT token
            var token = await _userService.GenerateJwt(user);

            return Ok(new GoogleAuthResponse
            {
                Success = true,
                Message = "Authenticated with Google",
                User = user,
                Token = token,
                IsNewUser = user.Id > 0
            });
        }
        catch (InvalidJwtException ex)
        {
            Console.WriteLine($"[GoogleAuth] InvalidJwtException: {ex.Message}");
            return Unauthorized(new { message = "Invalid Google token", error = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoogleAuth] Exception: {ex}");
            return StatusCode(500, new { message = "Internal server error during Google authentication" });
        }
    }
}

