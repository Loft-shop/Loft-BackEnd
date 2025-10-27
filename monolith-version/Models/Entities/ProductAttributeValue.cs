using monolith_version.Models.Enums;
using System.Buffers;
using monolith_version.Models.Entities;

namespace monolith_version.Models.Entities
{
    public class ProductAttributeValue
    {
        public int Id { get; set; }
        // Уникальный идентификатор записи (PK)

        public int ProductId { get; set; }
        // FK на товар

        public Product Product { get; set; } = null!;
        // Навигационное свойство на товар

        public int AttributeId { get; set; }
        // FK на атрибут

        public AttributeEntity Attribute { get; set; } = null!;
        // Навигационное свойство на атрибут

        public string Value { get; set; } = null!;
        // Значение атрибута, заполненное пользователем
    }

}
