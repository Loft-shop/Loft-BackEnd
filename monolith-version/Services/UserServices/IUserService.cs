using monolith_version.DTOs;

namespace monolith_version.Services.UserServices;

public interface IUserService
{
    Task<UserDto?> GetUserById(long userId);
    Task<UserDto?> GetUserByEmail(string email);
    Task<UserDto> CreateUser(UserDto user, string password);
    Task<UserDto?> UpdateUser(long userId, UserDto user);
    Task DeleteUser(long userId);
    Task<bool> IsEmailTaken(string email);
    Task<UserDto?> AuthenticateUser(string email, string password);
    Task<string> GenerateJwt(UserDto user);
    Task<UserDto?> ToggleSellerStatus(long userId);
    Task<bool> CanUserSell(long userId);
}
