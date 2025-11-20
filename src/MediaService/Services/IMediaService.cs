using Loft.Common.DTOs;
using MediaService.Entities;

namespace MediaService.Services
{
    public interface IMediaService
    {
        Task<MediaFile> UploadFileAsync(IFormFile file, string category, int uploadedBy, bool isPrivate = false);
        Task<DownloadToken> GenerateDownloadTokenAsync(Guid mediaId, TimeSpan validFor);
        Task<(byte[] content, string contentType, string originalName)> DownloadFileByTokenAsync(string token);
        Task<bool> DeleteFileAsync(Guid mediaId, int requestingUserId);
        Task<bool> DeleteFileByUrlAsync(string fileUrl, int requestingUserId);
        IEnumerable<MediaFile> GetFilesByUser(int userId);
    }
}

