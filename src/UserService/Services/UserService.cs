using AutoMapper;
using Loft.Common.DTOs;
using Loft.Common.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UserService.Data;
using UserService.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace UserService.Services;


public class UserService : IUserService
{
    private readonly UserDbContext _db;
    private readonly IMapper _mapper;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _smtpFrom;


    public UserService(UserDbContext db, IMapper mapper, ITokenService tokenService, IConfiguration configuration, ILogger<UserService> logger)
    {
        _db = db;
        _mapper = mapper;
        _tokenService = tokenService;
        _passwordHasher = new PasswordHasher<User>();
        _logger = logger;

        _configuration = configuration;

        // Загружаем настройки почты из конфигурации
        _smtpHost = _configuration["Email:Smtp:Host"];
        _smtpPort = int.Parse(_configuration["Email:Smtp:Port"]);
        _smtpUser = _configuration["Email:Smtp:User"];
        _smtpPass = _configuration["Email:Smtp:Pass"];
        _smtpFrom = _configuration["Email:Smtp:From"];
    }

    public async Task<UserDTO?> GetUserById(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user == null ? null : _mapper.Map<UserDTO>(user);
    }

    public async Task<UserDTO?> GetUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var normalized = email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
        return user == null ? null : _mapper.Map<UserDTO>(user);
    }

    public async Task<UserDTO> CreateUser(UserDTO userDto, string password)
    {
        if (userDto == null) throw new ArgumentNullException(nameof(userDto));
        if (string.IsNullOrWhiteSpace(userDto.Email)) throw new ArgumentException("Email is required", nameof(userDto));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required", nameof(password));

        var email = userDto.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            throw new InvalidOperationException("Email already taken");

        var user = new User
        {
            Email = email,
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            AvatarUrl = userDto.AvatarUrl,
            Phone = userDto.Phone,
            Role = userDto.Role,
            CanSell = userDto.CanSell
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return _mapper.Map<UserDTO>(user);
    }

    public async Task<UserDTO?> UpdateUser(long userId, UserDTO user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        var existing = await _db.Users.FindAsync(userId);
        if (existing == null) return null;

        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.AvatarUrl = user.AvatarUrl;
        existing.Phone = user.Phone;
        // Email/Role изменения опускаем для безопасности

        await _db.SaveChangesAsync();
        return _mapper.Map<UserDTO>(existing);
    }

    public async Task DeleteUser(long userId)
    {
        var existing = await _db.Users.FindAsync(userId);
        if (existing != null)
        {
            _db.Users.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsEmailTaken(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var normalized = email.Trim().ToLower();
        return await _db.Users.AnyAsync(u => u.Email.ToLower() == normalized);
    }

    public async Task<UserDTO?> AuthenticateUser(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        var normalized = email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
        if (user == null) return null;
        if (string.IsNullOrEmpty(user.PasswordHash)) return null;

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Success || verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            return _mapper.Map<UserDTO>(user);
        }
        return null;
    }

    public Task<string> GenerateJwt(UserDTO user)
    {
        var token = _tokenService.GenerateToken(user);
        return Task.FromResult(token);
    }

    public async Task<UserDTO?> ToggleSellerStatus(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;
        user.CanSell = !user.CanSell;
        await _db.SaveChangesAsync();
        return _mapper.Map<UserDTO>(user);
    }

    public async Task<bool> CanUserSell(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.CanSell ?? false;
    }

    public async Task<UserDTO?> GetUserByExternalProvider(string provider, string providerId)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerId))
            return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => 
            u.ExternalProvider == provider && u.ExternalProviderId == providerId);
        
        return user == null ? null : _mapper.Map<UserDTO>(user);
    }

    public async Task<UserDTO> CreateOrUpdateOAuthUser(string email, string provider, string providerId, 
        string? firstName = null, string? lastName = null, string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required", nameof(provider));
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID is required", nameof(providerId));

        var normalizedEmail = email.Trim().ToLower();

        // Check if user exists by external provider
        var user = await _db.Users.FirstOrDefaultAsync(u => 
            u.ExternalProvider == provider && u.ExternalProviderId == providerId);

        if (user != null)
        {
            // Update existing OAuth user
            user.Email = email;
            user.FirstName = firstName ?? user.FirstName;
            user.LastName = lastName ?? user.LastName;
            user.AvatarUrl = avatarUrl ?? user.AvatarUrl;
            
            await _db.SaveChangesAsync();
            return _mapper.Map<UserDTO>(user);
        }

        // Check if user exists by email (linking existing account)
        user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        
        if (user != null)
        {
            // Link external provider to existing user
            user.ExternalProvider = provider;
            user.ExternalProviderId = providerId;
            user.FirstName = firstName ?? user.FirstName;
            user.LastName = lastName ?? user.LastName;
            user.AvatarUrl = avatarUrl ?? user.AvatarUrl;
            
            await _db.SaveChangesAsync();
            return _mapper.Map<UserDTO>(user);
        }

        // Create new OAuth user
        var newUser = new User
        {
            Email = email,
            FirstName = firstName ?? email.Split('@')[0],
            LastName = lastName,
            AvatarUrl = avatarUrl,
            Role = Role.CUSTOMER,
            CanSell = false,
            ExternalProvider = provider,
            ExternalProviderId = providerId,
            PasswordHash = string.Empty // OAuth users don't have password
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        return _mapper.Map<UserDTO>(newUser);
    }

    // === Сброс пароля — отправка кода ===
    public async Task<bool> SendResetCodeAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);

        var code = GenerateNumericCode(6);
        var codeHash = ComputeSha256Hash(code);

        var ttlMinutes = 60; //    Время жизни кода в минутах
        var reset = new PasswordReset
        {
            Email = normalized,
            UserId = user?.Id,
            CodeHash = codeHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes),
            Used = false
        };

        _db.PasswordResets.Add(reset);
        await _db.SaveChangesAsync();

        if (user != null)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Loft", _smtpFrom));
                message.To.Add(MailboxAddress.Parse(user.Email));
                message.Subject = "Password reset - confirmation code";
                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <p>You have requested a password reset. Use the code below to change your password. The code is valid. {ttlMinutes} minutes.</p>
                        <h2 style=""font-family: monospace; background:#f0f0f0; padding:10px; display:inline-block;"">{code}</h2>
                        <p>If you did not request a password reset, please ignore this message..</p>"
                };

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset code to {Email}", user.Email);
            }
        }
        else
        {
            _logger.LogInformation("Password reset requested for unknown email {Email}", normalized);
        }

        return true;
    }

    // === Смена пароля по коду ===
    public async Task<bool> ResetPasswordWithCodeAsync(string email, string code, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(newPassword))
            return false;

        var normalized = email.Trim().ToLowerInvariant();
        var reset = await _db.PasswordResets
            .Where(p => p.Email == normalized && !p.Used && p.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (reset == null) return false;
        if (!string.Equals(ComputeSha256Hash(code), reset.CodeHash, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!reset.UserId.HasValue) return false;

        var user = await _db.Users.FindAsync(reset.UserId.Value);
        if (user == null) return false;

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        reset.Used = true;

        await _db.SaveChangesAsync();
        return true;
    }

    // === Вспомогательные методы ===
    private static string GenerateNumericCode(int digits)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var value = Math.Abs(BitConverter.ToInt32(bytes, 0));
        var max = (int)Math.Pow(10, digits);
        return (value % max).ToString().PadLeft(digits, '0');
    }

    private static string ComputeSha256Hash(string raw)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

}
