using Loft.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserService.Data;
using UserService.Entities;
using UserService.Services;

public class ChatService : IChatService
{
    private readonly UserDbContext _db;

    public ChatService(UserDbContext db)
    {
        _db = db;
    }

    public async Task<ChatMessageDTO> SendMessage(long senderId, long recipientId, string? messageText, string? fileUrl = null)
    {
        // Ищем чат между пользователями
        var chat = await _db.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c =>
                (c.User1Id == senderId && c.User2Id == recipientId) ||
                (c.User1Id == recipientId && c.User2Id == senderId));

        if (chat == null)
        {
            chat = new Chat
            {
                User1Id = senderId,
                User2Id = recipientId
            };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();
        }

        var message = new ChatMessage
        {
            ChatId = chat.Id,
            SenderId = senderId,
            MessageText = messageText,
            FileUrl = fileUrl
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        return new ChatMessageDTO
        {
            Id = message.Id,
            SenderId = senderId,
            RecipientId = recipientId,
            MessageText = messageText,
            FileUrl = fileUrl,
            IsRead = false,
            SentAt = message.SentAt
        };
    }

    public async Task<List<ChatMessageDTO>> GetConversation(long userId, long otherUserId)
    {
        var chat = await _db.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c =>
                (c.User1Id == userId && c.User2Id == otherUserId) ||
                (c.User1Id == otherUserId && c.User2Id == userId));

        if (chat == null) return new List<ChatMessageDTO>();

        return chat.Messages
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDTO
            {
                Id = m.Id,
                SenderId = m.SenderId,
                RecipientId = m.SenderId == chat.User1Id ? chat.User2Id : chat.User1Id,
                MessageText = m.MessageText,
                FileUrl = m.FileUrl,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            })
            .ToList();
    }

    public async Task MarkMessagesAsRead(long userId, long otherUserId)
    {
        var chat = await _db.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c =>
                (c.User1Id == userId && c.User2Id == otherUserId) ||
                (c.User1Id == otherUserId && c.User2Id == userId));

        if (chat == null) return;

        var unread = chat.Messages
            .Where(m => m.SenderId == otherUserId && !m.IsRead);

        foreach (var msg in unread)
        {
            msg.IsRead = true;
        }

        await _db.SaveChangesAsync();
    }
    // Получение чата по его идентификатору
    public async Task<ChatDTO?> GetChatById(long chatId)
    {
        var chat = await _db.Chats.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == chatId);
        if (chat == null) return null;
        var lastMsg = chat.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
        return new ChatDTO
        {
            ChatId = chat.Id,
            User1Id = chat.User1Id,
            User2Id = chat.User2Id,
            CreatedAt = chat.CreatedAt,
            LastMessage = lastMsg == null ? null : new ChatMessageDTO
            {
                Id = lastMsg.Id,
                SenderId = lastMsg.SenderId,
                RecipientId = lastMsg.SenderId == chat.User1Id ? chat.User2Id : chat.User1Id,
                MessageText = lastMsg.MessageText,
                FileUrl = lastMsg.FileUrl,
                IsRead = lastMsg.IsRead,
                SentAt = lastMsg.SentAt
            }
        };
    }
    // Удаление чата по его идентификатору
    public async Task DeleteChat(long chatId)
    {
        var chat = await _db.Chats.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == chatId);
        if (chat != null)
        {
            _db.Chats.Remove(chat);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<ChatDTO>> GetUserChats(long userId)
    {
        var chats = await _db.Chats
            .Include(c => c.Messages)
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .ToListAsync();

        return chats.Select(c =>
        {
            var lastMsg = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
            return new ChatDTO
            {
                ChatId = c.Id,
                User1Id = c.User1Id,
                User2Id = c.User2Id,
                CreatedAt = c.CreatedAt,
                LastMessage = lastMsg == null ? null : new ChatMessageDTO
                {
                    Id = lastMsg.Id,
                    SenderId = lastMsg.SenderId,
                    RecipientId = lastMsg.SenderId == c.User1Id ? c.User2Id : c.User1Id,
                    MessageText = lastMsg.MessageText,
                    FileUrl = lastMsg.FileUrl,
                    IsRead = lastMsg.IsRead,
                    SentAt = lastMsg.SentAt
                }
            };
        }).ToList();
    }
}