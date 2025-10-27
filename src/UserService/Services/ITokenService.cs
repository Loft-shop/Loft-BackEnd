namespace UserService.Services;

public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token string for the given user DTO.
    /// Returns null on failure.
    /// </summary>
    string GenerateToken(Loft.Common.DTOs.UserDTO user);
}