using System.ComponentModel.DataAnnotations;

namespace monolith_version.DTOs.Auth;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }
}

