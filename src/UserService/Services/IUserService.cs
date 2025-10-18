using System.Threading.Tasks;
using Loft.Common.DTOs;

namespace UserService.Services;

public interface IUserService
{
    Task<UserDTO?> GetUserById(long userId);
    Task<UserDTO?> GetUserByEmail(string email);
    Task<UserDTO> CreateUser(UserDTO user);
    Task<UserDTO?> UpdateUser(long userId, UserDTO user);
    Task DeleteUser(long userId);
    Task<bool> IsEmailTaken(string email);
    Task<UserDTO?> AuthenticateUser(string email, string password);
}