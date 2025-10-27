namespace monolith_version.DTOs;
using monolith_version.Models.Enums;

public record UserDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.CUSTOMER; // по умолчанию CUSTOMER
    public string AvatarUrl { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool CanSell { get; set; } = false;

    // ѕустой конструктор нужен AutoMapper
    public UserDto() { }

    // ƒл€ удобства можно оставить конструктор с параметрами
    public UserDto(long id, string name, string email, Role role,
                   string avatarUrl, string firstName, string lastName, string phone, bool canSell)
    {
        Id = id;
        Name = name;
        Email = email;
        Role = role;
        AvatarUrl = avatarUrl;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        CanSell = canSell;
    }
}
