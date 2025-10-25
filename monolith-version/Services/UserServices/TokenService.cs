using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using monolith_version.DTOs;

namespace monolith_version.Services.UserServices;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpireMinutes { get; set; } = 60;
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IConfiguration configuration)
    {
        _settings = new JwtSettings();
        configuration.GetSection("Jwt").Bind(_settings);
        if (string.IsNullOrWhiteSpace(_settings.Key))
        {
            // fallback dev key
            _settings.Key = "DevKey_ChangeMe_ForLocalOnly_1234567890";
        }
        if (_settings.ExpireMinutes <= 0)
        {
            _settings.ExpireMinutes = 60;
        }
    }

    public string GenerateToken(UserDto user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role ?? string.Empty),
            new("canSell", user.CanSell.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpireMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
