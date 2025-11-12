using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using UserService.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace UserService.Hubs
{
    // Hub для реального времени сообщений
    public class ChatHub : Hub
    {
        // Когда пользователь подключается к Hub
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromClaims();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());
            }

            //Console.WriteLine($"✅ Пользователь {userId} подключился. ConnectionId = {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }

        protected long? GetUserIdFromClaims()
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
                var claim = Context.User.FindFirst(t)?.Value;
                if (!string.IsNullOrEmpty(claim) && long.TryParse(claim, out var id)) return id;
            }

            // Резервный метод: найти любой числовой claim в токене
            foreach (var c in Context.User.Claims)
            {
                if (!string.IsNullOrEmpty(c.Value) && long.TryParse(c.Value, out var id)) return id;
            }

            return null;
        }
    }

}
