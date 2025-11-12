using System.Collections.Generic;
using System.Threading.Tasks;
using Loft.Common.DTOs;

namespace UserService.Services
{
    public interface IChatService
    {
        // Отправка сообщения от senderId к recipientId с текстом и/или файлом
        Task<ChatMessageDTO> SendMessage(long senderId, long recipientId, string? messageText, string? fileUrl = null);
        // Получение всей переписки между двумя пользователями
        Task<List<ChatMessageDTO>> GetConversation(long userId, long otherUserId);
        // Пометить все сообщения от otherUserId к userId как прочитанные
        Task MarkMessagesAsRead(long userId, long otherUserId);
        // Получение списка чатов пользователя с последними сообщениями
        Task<List<ChatDTO>> GetUserChats(long userId);
        // Получение чата по его идентификатору
        Task<ChatDTO?> GetChatById(long chatId);

        // Удаление чата по его идентификатору
        Task DeleteChat(long chatId);
    }
}
