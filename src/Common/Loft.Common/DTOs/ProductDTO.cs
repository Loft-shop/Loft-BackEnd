using Loft.Common.Enums;

// DTO для отображения товара (чтение)
namespace Loft.Common.DTOs
{
    public class ProductDto
    {
        public int? Id { get; set; }
        public int? IdUser { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public ProductType Type { get; set; }
        public decimal Price { get; set; }
        public CurrencyType Currency { get; set; }
        public int Quantity { get; set; }
        public int? ViewCount { get; set; }
        public ModerationStatus? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Атрибуты товара
        public ICollection<ProductAttributeValueDto>? AttributeValues { get; set; }

        // Медиа-файлы
        public ICollection<MediaFileDto>? MediaFiles { get; set; }

        // Комментарии (опционально)
        public ICollection<CommentDto>? Comments { get; set; }
    }

    public class ProductAttributeValueDto
    {
        public int AttributeId { get; set; }
        public string Value { get; set; } = null!;
    }

    public class MediaFileDto
    {
        public string Url { get; set; } = null!;
        public MediaTyp MediaTyp { get; set; }
    }

    public class CommentDto
    {
        public string UserId { get; set; } = null!;
        public string Text { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public ICollection<MediaFileDto>? MediaFiles { get; set; }
    }
}