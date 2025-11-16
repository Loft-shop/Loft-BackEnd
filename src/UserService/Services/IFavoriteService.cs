using Loft.Common.DTOs;

namespace UserService.Services
{
    public interface IFavoriteService
    {
        // Получить все избранные товары пользователя
        Task<FavoritesListDto> GetFavoritesAsync(long userId);
        // Добавить товар в избранное
        Task AddFavoriteAsync(long userId, int productId);
        // Удалить товар из избранного
        Task RemoveFavoriteAsync(long userId, int productId);
    }
}
