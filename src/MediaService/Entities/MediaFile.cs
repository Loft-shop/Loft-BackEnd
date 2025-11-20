using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediaService.Entities
{
    [Table("media_files")]
    public class MediaFile
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required, MaxLength(255)] public string OriginalName { get; set; } = null!;
        [Required, MaxLength(255)] public string StoredName { get; set; } = null!;
        [Required, MaxLength(10)] public string Extension { get; set; } = null!;
        [Required, MaxLength(100)] public string ContentType { get; set; } = null!;
        [Required] public long FileSize { get; set; }
        [Required, MaxLength(500)] public string RelativePath { get; set; } = null!;
        [MaxLength(500)] public string? Url { get; set; } // для публичных файлов
        [Required] public bool IsPrivate { get; set; } = false;
        [Required] public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int? UploadedBy { get; set; }
        [Required, MaxLength(50)] public string FileType { get; set; } = "image";
    }
}

