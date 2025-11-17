using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace MediaService.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly ILogger<ImageProcessingService> _logger;

        public ImageProcessingService(ILogger<ImageProcessingService> logger)
        {
            _logger = logger;
        }

        public async Task<string> CreateThumbnailAsync(string filePath, int width, int height)
        {
            try
            {
                var thumbnailPath = Path.Combine(
                    Path.GetDirectoryName(filePath)!,
                    "thumbnails",
                    $"thumb_{Path.GetFileName(filePath)}"
                );

                var thumbnailDir = Path.GetDirectoryName(thumbnailPath);
                if (!Directory.Exists(thumbnailDir))
                {
                    Directory.CreateDirectory(thumbnailDir!);
                }

                using var image = await Image.LoadAsync(filePath);
                
                // Создаем миниатюру с сохранением пропорций
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                }));

                await image.SaveAsync(thumbnailPath);

                _logger.LogInformation($"Thumbnail created: {thumbnailPath}");
                return thumbnailPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating thumbnail for {filePath}");
                throw;
            }
        }

        public async Task<bool> IsValidImageAsync(Stream stream)
        {
            try
            {
                var info = await Image.IdentifyAsync(stream);
                return info != null;
            }
            catch
            {
                return false;
            }
        }

        public string GetImageFormat(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}

