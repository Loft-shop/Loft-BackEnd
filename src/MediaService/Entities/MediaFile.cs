using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediaService.Entities
{
    public class MediaFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        // Уникальный идентификатор файла (PK)

        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = null!;
        // Уникальное имя файла на диске

        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = null!;
        // Полный путь к файлу на диске

        [Required]
        [MaxLength(1000)]
        public string FileUrl { get; set; } = null!;
        // URL для доступа к файлу

        [MaxLength(1000)]
        public string? ThumbnailUrl { get; set; }
        // URL миниатюры (если есть)

        public long FileSize { get; set; }
        // Размер файла в байтах

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = null!;
        // MIME тип файла (image/jpeg, image/png и т.д.)

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "general";
        // Категория файла (avatars, products, general и т.д.)

        public long? UserId { get; set; }
        // ID пользователя, загрузившего файл (опционально)

        public string? RelatedEntityType { get; set; }
        // Тип связанной сущности (User, Product и т.д.)

        public long? RelatedEntityId { get; set; }
        // ID связанной сущности

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Дата создания файла

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Дата последнего обновления

        public bool IsDeleted { get; set; } = false;
        // Флаг мягкого удаления

        public DateTime? DeletedAt { get; set; }
        // Дата удаления (если удален)
    }
}

