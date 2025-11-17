namespace MediaService.Services
{
    public interface IImageProcessingService
    {
        Task<string> CreateThumbnailAsync(string filePath, int width, int height);
        Task<bool> IsValidImageAsync(Stream stream);
        string GetImageFormat(string fileName);
    }
}

