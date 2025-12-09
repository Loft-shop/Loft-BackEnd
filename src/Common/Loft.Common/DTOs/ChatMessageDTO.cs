using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loft.Common.DTOs
{
    // DTO для отправки сообщения с фронтенда на сервер
    public record SendMessageRequest(long RecipientId, string? MessageText, string? FileUrl);

    public record ChatMessageDTO
    {
        // DTO для передачи сообщения с сервера на фронтенд
        public long Id { get; set; }    // Unique identifier for the chat message
        public long SenderId { get; set; } // ID of the user who sent the message
        public long RecipientId { get; set; } // ID of the user who is the recipient of the message
        public string MessageText { get; set; } = string.Empty; // Text content of the message
        public string? FileUrl { get; set; } // Optional URL to an attached file
        public bool IsRead { get; set; } = false; // Indicates if the message has been read
        public DateTime SentAt { get; set; } = DateTime.UtcNow; // Timestamp when the message was sent
        public bool IsMod { get; set; } = false; // moderator flag
    }

    public record ChatDTO
    {
        public long ChatId { get; set; }
        public long User1Id { get; set; }
        public long User2Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public ChatMessageDTO? LastMessage { get; set; }
    }
}
