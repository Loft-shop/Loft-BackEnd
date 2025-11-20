using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediaService.Entities
{
        [Table("download_tokens")]
        public class DownloadToken
        {
            [Key] public Guid Id { get; set; } = Guid.NewGuid();
            [Required] public Guid MediaId { get; set; }
            [ForeignKey("MediaId")] public MediaFile MediaFile { get; set; } = null!;
            [Required, MaxLength(255)] public string Token { get; set; } = null!;
            [Required] public DateTime ExpiresAt { get; set; }
            [Required] public bool IsUsed { get; set; } = false;
            [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
}
