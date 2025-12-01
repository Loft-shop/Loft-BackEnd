namespace UserService.Entities
{
    public class PasswordReset
    {
        public long Id { get; set; }
        public long? UserId { get; set; } // Связь с пользователем
        public string Email { get; set; } = string.Empty;
        public string CodeHash { get; set; } = string.Empty;
        public bool Used { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
