namespace UserService.Entities
{
    public class Chat
    {
        public long Id { get; set; }
        public long User1Id { get; set; } // Обычно продавец
        public long User2Id { get; set; } // Покупатель
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
