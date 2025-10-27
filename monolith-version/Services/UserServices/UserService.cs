using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using monolith_version.Data;
using monolith_version.DTOs;
using monolith_version.Models.Entities;
using monolith_version.Models.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace monolith_version.Services.UserServices;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public UserService(AppDbContext db, IMapper mapper, ITokenService tokenService)
    {
        _db = db;
        _mapper = mapper;
        _tokenService = tokenService;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<UserDto?> GetUserById(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var normalized = email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateUser(UserDto userDto, string password)
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

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> UpdateUser(long userId, UserDto user)
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
        return _mapper.Map<UserDto>(existing);
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

    public async Task<UserDto?> AuthenticateUser(string email, string password)
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
            return _mapper.Map<UserDto>(user);
        }
        return null;
    }

    public Task<string> GenerateJwt(UserDto user)
    {
        var token = _tokenService.GenerateToken(user);
        return Task.FromResult(token);
    }

    public async Task<UserDto?> ToggleSellerStatus(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;
        user.CanSell = !user.CanSell;
        await _db.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> CanUserSell(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.CanSell ?? false;
    }
}
