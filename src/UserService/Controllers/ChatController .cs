using Loft.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using UserService.Services;


[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    // Отправить сообщение другому пользователю
    [HttpPost("send")]
    public async Task<ActionResult<ChatMessageDTO>> SendMessage([FromBody] SendMessageRequest request)
    {
        long senderId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User ID not found in token.");
        var message = await _chatService.SendMessage(senderId, request.RecipientId, request.MessageText, request.FileUrl);
        return Ok(message);
    }

    // Получить переписку с другим пользователем
    [HttpGet("conversation/{otherUserId}")]
    public async Task<ActionResult<List<ChatMessageDTO>>> GetConversation(long otherUserId)
    {
        long userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User ID not found in token.");
        var messages = await _chatService.GetConversation(userId, otherUserId);
        return Ok(messages);
    }

    // Пометить сообщения от другого пользователя как прочитанные
    [HttpPost("mark-read/{otherUserId}")]
    public async Task<IActionResult> MarkRead(long otherUserId)
    {
        long userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User ID not found in token.");
        await _chatService.MarkMessagesAsRead(userId, otherUserId);
        return Ok();
    }

    // Получить список чатов текущего пользователя
    [HttpGet("my-chats")]
    public async Task<ActionResult<List<ChatDTO>>> GetMyChats()
    {
        long userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User ID not found in token.");
        var chats = await _chatService.GetUserChats(userId);
        return Ok(chats);
    }

    [HttpDelete("{chatId}")]
    public async Task<IActionResult> DeleteChat(long chatId)
    {
        long userId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User ID not found in token.");

        // Проверяем, принадлежит ли чат текущему пользователю
        var chat = await _chatService.GetChatById(chatId);
        if (chat == null) return NotFound("Chat not found");
        if (chat.User1Id != userId && chat.User2Id != userId)
            return Forbid("You are not allowed to delete this chat");

        await _chatService.DeleteChat(chatId);
        return NoContent();
    }

    protected long? GetCurrentUserId()
    {
        // Попробуем несколько общих типов соответственно возможным картам claim-ов:
        var tryTypes = new[]
        {
            ClaimTypes.NameIdentifier, // "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            "nameid",                  // иногда сокращённое имя
            JwtRegisteredClaimNames.Sub, // "sub"
            "id",
            "user_id",
            ClaimTypes.Name,
            ClaimTypes.Email
        };

        foreach (var t in tryTypes)
        {
            var claim = User.FindFirst(t)?.Value;
            if (!string.IsNullOrEmpty(claim) && long.TryParse(claim, out var id)) return id;
        }

        // Резервный метод: найти любой числовой claim в токене
        foreach (var c in User.Claims)
        {
            if (!string.IsNullOrEmpty(c.Value) && long.TryParse(c.Value, out var id)) return id;
        }

        return null;
    }
}
