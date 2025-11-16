using System;
using Loft.Common.Enums;

namespace Loft.Common.DTOs
{
    public class FavoritesListDto
    {
        // Список ID избранных товаров
        public List<int> ProductIds { get; set; } = new List<int>();
    }
}