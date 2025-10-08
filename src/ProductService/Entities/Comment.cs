using System.Buffers;
using Loft.Common.Enums;

namespace ProductService.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        // Уникальный идентификатор комментария

        public int ProductId { get; set; }
        // FK на товар

        public Product Product { get; set; } = null!;
        // Навигационное свойство на товар

        public string UserId { get; set; } = null!;
        // Идентификатор пользователя, оставившего комментарий

        public string Text { get; set; } = null!;
        // Текст комментария

        public ModerationStatus Status { get; set; }
        // Статус модерации комментария: Pending / Approved / Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Дата создания комментария

        public ICollection<MediaFile>? MediaFiles { get; set; }
        // Медиафайлы (изображения/видео), прикреплённые к комментарию
    }

}
