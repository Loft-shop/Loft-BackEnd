using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserService.Entities;

namespace ProductService.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected long? GetUserId()
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

        protected bool IsModerator()
        {
            var roles = GetUserRoles();
            return roles.Contains("MODERATOR"); 
        }

        protected List<string> GetUserRoles()
        {
            return User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }
    }
}
