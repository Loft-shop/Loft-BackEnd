namespace UserService.DTOs
{
    public class ResetPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ConfirmResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
