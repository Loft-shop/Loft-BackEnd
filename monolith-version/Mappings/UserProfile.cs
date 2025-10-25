using AutoMapper;
using monolith_version.DTOs;
using monolith_version.Models.Entities;

namespace monolith_version.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ConstructUsing(src => new UserDto(
                src.Id,
                string.Join(' ', new[] { src.FirstName, src.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                src.Email,
                src.Role.ToString(),
                src.AvatarUrl ?? string.Empty,
                src.FirstName ?? string.Empty,
                src.LastName ?? string.Empty,
                src.Phone ?? string.Empty,
                src.CanSell
            ));
    }
}
