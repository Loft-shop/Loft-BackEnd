using monolith_version.Models.Enums;
using System.Buffers;
using System.ComponentModel;

namespace monolith_version.Models.Entities
{
    public class Category
    {
        public int Id { get; set; }
        // Уникальный идентификатор категории (PK)

        public int? ParentCategoryId { get; set; }
        // Ссылка на родительскую категорию для подкатегорий (nullable, если верхний уровень)

        public Category? ParentCategory { get; set; }
        // Навигационное свойство EF для связи с родительской категорией
        public ICollection<Category>? SubCategories { get; set; }
        // Навигационное свойство EF для связи с дочерними категориями

        public string Name { get; set; } = null!;
        // Название категории, отображаемое пользователю
        public string? ImageUrl { get; set; } = null!;
        // Изображения категории, отображаемое пользователю
        public ModerationStatus Status { get; set; }
        // Статус модерации категории: Pending (на модерации), Approved (одобрено), Rejected (отклонено)

        public int ViewCount { get; set; } = 0;
        // Счётчик просмотров категории для статистики

        public ICollection<CategoryAttribute>? CategoryAttributes { get; set; }
        // Список связей с атрибутами (какие атрибуты привязаны к категории)

        public ICollection<Product>? Products { get; set; }
        // Список товаров, которые принадлежат этой категории
    }
}
