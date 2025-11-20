using Microsoft.AspNetCore.Http;

namespace Loft.Common.DTOs;


public class MyPublicMediaFileDTO
{
    public Guid Id { get; set; } // Уникальный идентификатор файла
    public string OriginalName { get; set; } = null!; // Оригинальное имя файла
    public string Url { get; set; } = null!; // Публичный URL для доступа к файлу
    public string FileType { get; set; } = null!; // Тип файла (например, "image", "video", "document" и т.д.)
    public long FileSize { get; set; } // Размер файла в байтах
    public DateTime UploadedAt { get; set; } // Дата и время загрузки файла
}

public class UploadFileDto
{
    // Файл для загрузки
    public IFormFile File { get; set; } = null!;
    // Путь файла относительно корневой директории хранилища
    public string Category { get; set; } = null!;
    // Флаг приватности файла
    public bool IsPrivate { get; set; } = false;
}