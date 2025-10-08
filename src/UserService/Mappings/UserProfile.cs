using System.Linq;
using AutoMapper;
using Loft.Common.DTOs;
using UserService.Entities;

namespace UserService.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDTO>()
            .ConstructUsing(src => new UserDTO(
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