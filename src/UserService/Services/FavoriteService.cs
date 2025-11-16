using Loft.Common.DTOs;
using System;
using UserService.Data;
using Microsoft.EntityFrameworkCore;

namespace UserService.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly UserDbContext _context;

        public FavoriteService(UserDbContext context)
        {
            _context = context;
        }

        // Получить все избранные товары пользователя
        public async Task<FavoritesListDto> GetFavoritesAsync(long userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            return new FavoritesListDto
            {
                ProductIds = user?.FavoriteProductIds?.ToList() ?? new List<int>()
            };
        }

        // Добавить товар в избранное
        public async Task AddFavoriteAsync(long userId, int productId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            // конвертируем массив в список для удобства работы
            var list = user.FavoriteProductIds?.ToList() ?? new List<int>();

            if (!list.Contains(productId))
            {
                list.Add(productId);
                user.FavoriteProductIds = list.ToArray(); // обратно в массив для EF
                await _context.SaveChangesAsync();
            }
        }

        // Удалить товар из избранного
        public async Task RemoveFavoriteAsync(long userId, int productId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            var list = user.FavoriteProductIds?.ToList() ?? new List<int>();

            if (list.Contains(productId))
            {
                list.Remove(productId);
                user.FavoriteProductIds = list.ToArray();
                await _context.SaveChangesAsync();
            }
        }
    }
}
