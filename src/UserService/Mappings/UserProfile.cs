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
        CreateMap<User, UserDTO>();

        // --- UserDto -> User ---
        CreateMap<UserDTO, User>();
    }
}
