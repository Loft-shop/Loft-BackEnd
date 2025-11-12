using System.Linq;
using AutoMapper;
using Loft.Common.DTOs;
using UserService.Entities;

namespace UserService.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // --- User -> UserDto ---
        CreateMap<User, UserDTO>().ReverseMap();

        // --- UserDto -> User ---
        CreateMap<UserDTO, User>();

        // --- UserDTO -> PublicUserDTO ---
        CreateMap<UserDTO, PublicUserDTO>();
    }
}
