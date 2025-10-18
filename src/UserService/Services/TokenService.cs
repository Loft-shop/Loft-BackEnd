using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Loft.Common.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace UserService.Services;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; }
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _settings = new JwtSettings();
        configuration.GetSection("Jwt").Bind(_settings);
        _logger = logger;
        if (string.IsNullOrWhiteSpace(_settings.Key))
        {
            _logger.LogWarning("JWT key is empty. Tokens cannot be generated without a secret key. Check appsettings.json or environment variables.");
        }
        else
        {
            _logger.LogInformation("JWT key length: {len}", _settings.Key.Length);
        }
    }

    public string GenerateToken(UserDTO user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        try
        {
            if (string.IsNullOrWhiteSpace(_settings.Key))
            {
                throw new InvalidOperationException("JWT key is not configured.");
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim("canSell", user.CanSell.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(_settings.ExpireMinutes > 0 ? _settings.ExpireMinutes : 60);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT for user {email}", user?.Email);
            throw;
        }
    }
}