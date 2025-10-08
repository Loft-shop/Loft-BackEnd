using System;
using System.Threading.Tasks;
using AutoMapper;
using Loft.Common.DTOs;
using UserService.Entities;
using UserService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Loft.Common.Enums;

namespace UserService.Services;


public class UserService : IUserService
{
    private readonly UserDbContext _db;
    private readonly IMapper _mapper;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public UserService(UserDbContext db, IMapper mapper, ITokenService tokenService)
    {
        _db = db;
        _mapper = mapper;
        _tokenService = tokenService;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<UserDTO?> GetUserById(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;
        return _mapper.Map<UserDTO>(user);
    }

    public async Task<UserDTO?> GetUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());
        if (user == null) return null;
        return _mapper.Map<UserDTO>(user);
    }

    public async Task<UserDTO> CreateUser(UserDTO userDto, string password)
    {
        if (userDto == null) throw new ArgumentNullException(nameof(userDto));
        if (string.IsNullOrWhiteSpace(userDto.Email)) throw new ArgumentException("Email is required", nameof(userDto));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required", nameof(password));

        var email = userDto.Email.Trim();

        // check existing
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            throw new InvalidOperationException("Email already taken");

        var user = new User
        {
            Email = email,
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            AvatarUrl = userDto.AvatarUrl,
            Phone = userDto.Phone,
            Role = Role.CUSTOMER,
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
        // Role and Email updates are omitted here; implement carefully if needed

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
        return await _db.Users.AnyAsync(u => u.Email.ToLower() == email.Trim().ToLower());
    }

    public async Task<UserDTO?> AuthenticateUser(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());
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
}