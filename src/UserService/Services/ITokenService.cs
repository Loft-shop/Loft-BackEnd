namespace UserService.Services;
using Loft.Common.DTOs;

public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token string for the given user DTO.
    /// Returns null on failure.
    /// </summary>
    string GenerateToken(UserDTO user);
}