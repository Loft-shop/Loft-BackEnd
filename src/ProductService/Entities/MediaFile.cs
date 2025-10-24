using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;
using Loft.Common.Enums;

namespace ProductService.Entities
{
    public class MediaFile
    {
        public int Id { get; set; }
        // Уникальный идентификатор медиафайла

        public int? ProductId { get; set; }
        // Если медиа принадлежит товару

        public Product? Product { get; set; }

        public int? CommentId { get; set; }
        // Если медиа принадлежит комментарию

        public Comment? Comment { get; set; }

        public string Url { get; set; } = null!;
        // Ссылка на изображение или видео

        public MediaTyp MediaTyp{ get; set; }
        // Тип медиа: Image / Video

        public ModerationStatus Status { get; set; }
        // Статус модерации медиа: Pending / Approved / Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Дата загрузки медиа
    }

}
