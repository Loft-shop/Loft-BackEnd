namespace Loft.Common.DTOs;

public record UserDTO(long Id, string Name, string Email, string Role, string AvatarUrl,string FirstName,string LastName,string Phone, bool CanSell);
