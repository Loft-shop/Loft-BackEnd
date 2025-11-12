namespace UserService.Entities
{
    public class ChatMessage
    {
        public long Id { get; set; }
        public long ChatId { get; set; }
        public Chat Chat { get; set; } = null!;

        public long SenderId { get; set; }
        public string? MessageText { get; set; }
        public string? FileUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
