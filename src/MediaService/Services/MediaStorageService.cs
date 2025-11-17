using Loft.Common.DTOs;
using MediaService.Entities;

namespace MediaService.Services
{
    public class MediaStorageService : IMediaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MediaStorageService> _logger;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly string _uploadPath;
        private readonly long _maxFileSizeBytes;
        private readonly List<string> _allowedImageExtensions;

        public MediaStorageService(
            IConfiguration configuration,
            ILogger<MediaStorageService> logger,
            IImageProcessingService imageProcessingService)
        {
            _configuration = configuration;
            _logger = logger;
            _imageProcessingService = imageProcessingService;

            // Получаем настройки из конфигурации
            var mediaSettings = _configuration.GetSection("MediaSettings");
            _uploadPath = mediaSettings.GetValue<string>("UploadPath") ?? "uploads";
            _maxFileSizeBytes = mediaSettings.GetValue<int>("MaxFileSizeMB") * 1024 * 1024;
            _allowedImageExtensions = mediaSettings.GetSection("AllowedImageExtensions").Get<List<string>>() 
                ?? new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            // Создаем директорию для загрузок, если её нет
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<UploadResponseDTO> UploadFileAsync(IFormFile file, string category = "general")
        {
            try
            {
                // Валидация файла
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is empty or null");
                }

                if (file.Length > _maxFileSizeBytes)
                {
                    throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSizeBytes / 1024 / 1024} MB");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(extension))
                {
                    throw new ArgumentException($"File extension {extension} is not allowed");
                }

                // Проверяем, что это действительно изображение
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                if (!await _imageProcessingService.IsValidImageAsync(memoryStream))
                {
                    throw new ArgumentException("Invalid image file");
                }

                // Создаем уникальное имя файла
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var categoryPath = Path.Combine(_uploadPath, category);
                
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                }

                var filePath = Path.Combine(categoryPath, uniqueFileName);

                // Сохраняем файл
                memoryStream.Position = 0;
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await memoryStream.CopyToAsync(fileStream);
                }

                _logger.LogInformation($"File uploaded: {filePath}");

                // Создаем миниатюру для изображений
                string? thumbnailPath = null;
                try
                {
                    var thumbnailWidth = _configuration.GetSection("MediaSettings").GetValue<int>("ThumbnailWidth");
                    var thumbnailHeight = _configuration.GetSection("MediaSettings").GetValue<int>("ThumbnailHeight");
                    thumbnailPath = await _imageProcessingService.CreateThumbnailAsync(filePath, thumbnailWidth, thumbnailHeight);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create thumbnail");
                }

                var response = new UploadResponseDTO
                {
                    Id = 0, // Будет обновлен после сохранения в БД
                    FileName = uniqueFileName,
                    FileUrl = $"/media/{category}/{uniqueFileName}",
                    ThumbnailUrl = thumbnailPath != null ? $"/media/{category}/thumbnails/thumb_{uniqueFileName}" : null,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    Category = category,
                    UploadedAt = DateTime.UtcNow
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                throw;
            }
        }

        public async Task<(Stream? stream, string? contentType, string? fileName)> DownloadFileAsync(string fileName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Ищем файл во всех категориях
                    var files = Directory.GetFiles(_uploadPath, fileName, SearchOption.AllDirectories);
                    
                    if (files.Length == 0)
                    {
                        return (null, null, null);
                    }

                    var filePath = files[0];
                    var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var contentType = _imageProcessingService.GetImageFormat(fileName);

                    return (stream, contentType, fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error downloading file: {fileName}");
                    return (null, null, null);
                }
            });
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Ищем файл во всех категориях
                    var files = Directory.GetFiles(_uploadPath, fileName, SearchOption.AllDirectories);
                    
                    if (files.Length == 0)
                    {
                        return false;
                    }

                    var filePath = files[0];
                    File.Delete(filePath);

                    // Удаляем миниатюру, если она есть
                    var thumbnailPath = Path.Combine(
                        Path.GetDirectoryName(filePath)!,
                        "thumbnails",
                        $"thumb_{fileName}"
                    );

                    if (File.Exists(thumbnailPath))
                    {
                        File.Delete(thumbnailPath);
                    }

                    _logger.LogInformation($"File deleted: {filePath}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting file: {fileName}");
                    return false;
                }
            });
        }

        public async Task<List<MediaFileDTO>> GetAllFilesAsync(string? category = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var searchPath = string.IsNullOrEmpty(category) 
                        ? _uploadPath 
                        : Path.Combine(_uploadPath, category);

                    if (!Directory.Exists(searchPath))
                    {
                        return new List<MediaFileDTO>();
                    }

                    var files = Directory.GetFiles(searchPath, "*.*", 
                        string.IsNullOrEmpty(category) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        .Where(f => !f.Contains("thumbnails")) // Исключаем миниатюры
                        .Select(f => new MediaFileDTO
                        {
                            Id = 0, // Временно, пока нет БД
                            FileName = Path.GetFileName(f),
                            FilePath = f,
                            FileUrl = $"/media/{GetRelativePath(_uploadPath, f)}",
                            ThumbnailUrl = null,
                            FileSize = new FileInfo(f).Length,
                            ContentType = _imageProcessingService.GetImageFormat(f),
                            Category = category ?? "general",
                            CreatedAt = File.GetCreationTimeUtc(f),
                            UpdatedAt = File.GetLastWriteTimeUtc(f)
                        })
                        .ToList();

                    return files;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting files list");
                    return new List<MediaFileDTO>();
                }
            });
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            return fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}

