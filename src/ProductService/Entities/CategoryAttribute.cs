using System.Buffers;
using Loft.Common.Enums;

namespace ProductService.Entities
{
    public class CategoryAttribute
    {
        public int Id { get; set; }
        // Уникальный идентификатор записи (PK)

        public int CategoryId { get; set; }
        // FK на категорию

        public Category Category { get; set; } = null!;
        // Навигационное свойство на категорию

        public int AttributeId { get; set; }
        // FK на атрибут

        public AttributeEntity Attribute { get; set; } = null!;
        // Навигационное свойство на атрибут

        public bool IsRequired { get; set; } = false;
        // Обязательный ли этот атрибут при создании товара

        public int OrderIndex { get; set; } = 0;
        // Порядок отображения атрибута в форме

        public ModerationStatus Status { get; set; }
        // Статус модерации связи: Pending / Approved / Rejected
    }

    // DTO для атрибута
    public class AttributeDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;          // Название атрибута
        public AttributeType Type { get; set; }            // Тип данных: String, Number, List
        public string TypeDisplayName { get; set; } = null!; // Отображение единицы измерения или типа ("кг", "см")
        public string? OptionsJson { get; set; }           // JSON-строка с вариантами для типа List
        public ModerationStatus? Status { get; set; }      // Статус модерации
    }
}