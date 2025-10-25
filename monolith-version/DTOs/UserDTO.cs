namespace monolith_version.DTOs;

public record UserDto(
    long Id,
    string Name,
    string Email,
    string Role,
    string AvatarUrl,
    string FirstName,
    string LastName,
    string Phone,
    bool CanSell
);
