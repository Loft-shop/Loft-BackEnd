using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using Loft.Common.Enums;

namespace ProductService.Entities
{
    public class AttributeEntity
    {
        public int Id { get; set; }
        // Уникальный идентификатор атрибута (PK)

        public string Name { get; set; } = null!;
        // название атрибута 

        public AttributeType Type { get; set; }
        // Тип данных: String (строка), Number (число), List (список вариантов)

        public string TypeDisplayName { get; set; } = null!;
        // Отображаемое имя типа атрибута ("кг", "см", "м2")

        public string? OptionsJson { get; set; }
        // Список вариантов для типа List в формате JSON, например ["Красный","Синий"]

        public ModerationStatus Status { get; set; }
        // Статус модерации атрибута: Pending / Approved / Rejected

        public ICollection<CategoryAttribute>? CategoryAttributes { get; set; }
        // Связь с категориями, к которым атрибут привязан

        public ICollection<ProductAttributeValue>? AttributeValues { get; set; }
        // Значения атрибута для товаров
    }
}