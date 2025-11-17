namespace Loft.Common.DTOs;

public record MediaFileDTO
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MediaFileDTO() { }

    public MediaFileDTO(long id, string fileName, string filePath, string fileUrl, 
                       string? thumbnailUrl, long fileSize, string contentType, 
                       string category, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        FileName = fileName;
        FilePath = filePath;
        FileUrl = fileUrl;
        ThumbnailUrl = thumbnailUrl;
        FileSize = fileSize;
        ContentType = contentType;
        Category = category;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}

