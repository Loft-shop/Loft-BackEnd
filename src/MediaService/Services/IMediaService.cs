using Loft.Common.DTOs;

namespace MediaService.Services
{
    public interface IMediaService
    {
        Task<UploadResponseDTO> UploadFileAsync(IFormFile file, string category = "general");
        Task<(Stream? stream, string? contentType, string? fileName)> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<List<MediaFileDTO>> GetAllFilesAsync(string? category = null);
    }
}

