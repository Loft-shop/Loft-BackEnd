using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public class GoogleAuthRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}

public class GoogleAuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Loft.Common.DTOs.UserDTO? User { get; set; }
    public string? Token { get; set; }
    public bool IsNewUser { get; set; }
}

