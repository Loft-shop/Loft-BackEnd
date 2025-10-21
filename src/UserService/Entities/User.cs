using System.ComponentModel.DataAnnotations;
using Loft.Common.Enums;

namespace UserService.Entities;
public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set;} = string.Empty;
    public Role Role { get; set; }
    public bool CanSell { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
}
