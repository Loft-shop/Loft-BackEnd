using System.Threading.Tasks;
using Loft.Common.DTOs;

namespace UserService.Services;


public interface IUserService
{
    Task<UserDTO?> GetUserById(long userId);
    Task<UserDTO?> GetUserByEmail(string email);
    Task<UserDTO> CreateUser(UserDTO user, string password);
    Task<UserDTO?> UpdateUser(long userId, UserDTO user);
    Task DeleteUser(long userId);
    Task<bool> IsEmailTaken(string email);
    Task<UserDTO?> AuthenticateUser(string email, string password);
    Task<string> GenerateJwt(UserDTO user);
    Task<UserDTO?> ToggleSellerStatus(long userId);
    Task<bool> CanUserSell(long userId);

    // OAuth methods
    Task<UserDTO?> GetUserByExternalProvider(string provider, string providerId);
    Task<UserDTO> CreateOrUpdateOAuthUser(string email, string provider, string providerId, string? firstName = null, string? lastName = null, string? avatarUrl = null);

    /// Генерирует код и отправляет письмо пользователю, если пользователь с таким email существует.
    /// Возвращает true если операция завершилась успешно (не означает что пользователь существует — ответ одинаковый для безопасности).
    Task<bool> SendResetCodeAsync(string email);

    /// Проверяет код и, если валидно, обновляет пароль пользователя.
    /// Возвращает true при успешной смене пароля.
    Task<bool> ResetPasswordWithCodeAsync(string email, string code, string newPassword);

}