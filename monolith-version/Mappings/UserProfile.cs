using AutoMapper;
using monolith_version.DTOs;
using monolith_version.Models.Entities;
using monolith_version.Models.Enums;

namespace monolith_version.Mappings;

public class UserProfile : Profile
{
        public UserProfile()
        {
        // --- User -> UserDto ---
        CreateMap<User, UserDto>();

        // --- UserDto -> User ---
        CreateMap<UserDto, User>();
    }
    }
