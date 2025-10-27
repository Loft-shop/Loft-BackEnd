using monolith_version.Models.Enums;
using System.Buffers;

namespace monolith_version.Models.Entities
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
}
