using monolith_version.DTOs;

namespace monolith_version.Services.UserServices;

public interface ITokenService
{
    string GenerateToken(UserDto user);
}
