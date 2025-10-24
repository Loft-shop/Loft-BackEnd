using Loft.Common.Enums;
using System.Buffers;
using UserService.Entities;

namespace ProductService.Entities
{
    public class Product
    {
        public int Id { get; set; }
        // Уникальный идентификатор товара (PK)

        public int IdUser { get; set; }
        // Уникальный идентификатор пользователя

        public int CategoryId { get; set; }
        // FK на категорию товара

        public Category Category { get; set; } = null!;
        // Навигационное свойство на категорию

        public string Name { get; set; } = null!;
        // Название товара

        public string? Description { get; set; }
        // Описание товара

        public ProductType Type { get; set; }
        // Тип товара: Physical (физический), Digital (цифровой)

        public decimal Price { get; set; }
        // Цена товара

        public CurrencyType Currency { get; set; }
        // Валюта цены: UAH, USD

        public int Quantity { get; set; }
        // Количество на складе

        public int ViewCount { get; set; } = 0;
        // Счётчик просмотров товара

        public ModerationStatus Status { get; set; }
        // Статус товара: Pending / Approved / Rejected / Sold

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Дата создания товара

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Дата последнего обновления

        public ICollection<ProductAttributeValue>? AttributeValues { get; set; }
        // Значения атрибутов товара

        public ICollection<MediaFile>? MediaFiles { get; set; }
        // Изображения и видео товара

        public ICollection<Comment>? Comments { get; set; }
        // Комментарии к товару
    }
}