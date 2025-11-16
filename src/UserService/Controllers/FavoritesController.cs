using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.Services;
using Loft.Common.DTOs;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Все методы доступны только авторизованным
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        // GET /api/favorites
        // Получить все избранные товары текущего пользователя
        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            FavoritesListDto favorites = await _favoriteService.GetFavoritesAsync(userId.Value);
            return Ok(favorites);
        }

        // POST /api/favorites/{productId}
        // Добавить товар в избранное
        [HttpPost("{productId}")]
        public async Task<IActionResult> AddFavorite(int productId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _favoriteService.AddFavoriteAsync(userId.Value, productId);
            return Ok();
        }

        // DELETE /api/favorites/{productId}
        // Удалить товар из избранного
        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFavorite(int productId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _favoriteService.RemoveFavoriteAsync(userId.Value, productId);
            return Ok();
        }

        // ---------------------------
        // Получаем userId из токена JWT
        private long? GetUserId()
        {
            var tryTypes = new[]
            {
                ClaimTypes.NameIdentifier,
                "nameid",
                "sub",
                "id",
                "user_id"
            };

            foreach (var t in tryTypes)
            {
                var claim = User.FindFirst(t)?.Value;
                if (!string.IsNullOrEmpty(claim) && long.TryParse(claim, out var id))
                    return id;
            }

            return null;
        }
    }
}
