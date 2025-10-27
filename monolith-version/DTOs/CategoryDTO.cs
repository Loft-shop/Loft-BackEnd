using monolith_version.Models.Enums;

namespace monolith_version.DTOs
{
    public class CategoryDto
    {
        public int? Id { get; set; }
        public int? ParentCategoryId { get; set; }
        public string Name { get; set; } = null!;

        public string? ImageUrl { get; set; } = null!;
        public ModerationStatus? Status { get; set; }

        public int? ViewCount { get; set; }

        // Атрибуты категории (какие атрибуты можно использовать для товаров)
        public ICollection<CategoryAttributeDto>? Attributes { get; set; }

        // Опционально: список подкатегорий
        public ICollection<CategoryDto>? SubCategories { get; set; }
    }

    public class CategoryAttributeDto
    {
        public int AttributeId { get; set; }
        public bool IsRequired { get; set; }
        public int OrderIndex { get; set; }
    }

    public class AttributeDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;          // общее название атрибута 
        public AttributeType Type { get; set; }            // Тип данных: String, Number, List
        public string TypeDisplayName { get; set; } = null!; // Отображение единицы измерения или типа ("кг", "см")
        public string? OptionsJson { get; set; }           // JSON-строка с вариантами для типа List
        public ModerationStatus? Status { get; set; }      // Статус модерации
    }
}