namespace Loft.Common.DTOs;

public record UploadResponseDTO
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    public UploadResponseDTO() { }

    public UploadResponseDTO(long id, string fileName, string fileUrl, string? thumbnailUrl, 
                            long fileSize, string contentType, string category, DateTime uploadedAt)
    {
        Id = id;
        FileName = fileName;
        FileUrl = fileUrl;
        ThumbnailUrl = thumbnailUrl;
        FileSize = fileSize;
        ContentType = contentType;
        Category = category;
        UploadedAt = uploadedAt;
    }
}

