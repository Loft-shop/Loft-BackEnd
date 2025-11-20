using Loft.Common.DTOs;
using MediaService.Data;
using MediaService.Entities;

namespace MediaService.Services
{
    public class MediaStorageService : IMediaService
    {

        private readonly MediaDbContext _db;
        private readonly string _storageRoot = Path.Combine(Directory.GetCurrentDirectory(), "storage");

        public MediaStorageService(MediaDbContext db) { _db = db; }

        public async Task<MediaFile> UploadFileAsync(IFormFile file, string category, int uploadedBy, bool isPrivate = false)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("File is empty");

            var extension = Path.GetExtension(file.FileName);
            var storedName = $"{Guid.NewGuid()}{extension}";

            // Выбираем корневую папку в зависимости от приватности
            var baseFolder = isPrivate ? "private" : "public";
            var categoryPath = Path.Combine(_storageRoot, baseFolder, category);

            if (!Directory.Exists(categoryPath))
                Directory.CreateDirectory(categoryPath);

            var path = Path.Combine(categoryPath, storedName);
            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            var media = new MediaFile
            {
                OriginalName = file.FileName,
                StoredName = storedName,
                Extension = extension,
                ContentType = file.ContentType,
                FileSize = file.Length,
                RelativePath = Path.Combine(baseFolder, category, storedName),
                IsPrivate = isPrivate,
                FileType = GetFileType(extension),
                Url = isPrivate ? null : $"/media/{category}/{storedName}", // публичный URL только для public
                UploadedBy = uploadedBy
            };

            _db.MediaFiles.Add(media);
            await _db.SaveChangesAsync();

            return media;
        }

        private string GetFileType(string ext)
        {
            ext = ext.ToLower();
            if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext)) return "image";
            if (new[] { ".mp4", ".avi", ".mov" }.Contains(ext)) return "video";
            if (new[] { ".pdf", ".doc", ".docx", ".zip" }.Contains(ext)) return "document";
            return "digital";
        }

        public async Task<DownloadToken> GenerateDownloadTokenAsync(Guid mediaId, TimeSpan validFor)
        {
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_").Replace("+", "-");

            var downloadToken = new DownloadToken
            {
                MediaId = mediaId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.Add(validFor)
            };

            _db.DownloadTokens.Add(downloadToken);
            await _db.SaveChangesAsync();
            return downloadToken;
        }

        public async Task<(byte[] content, string contentType, string originalName)> DownloadFileByTokenAsync(string token)
        {
            var t = _db.DownloadTokens
                .FirstOrDefault(x => x.Token == token && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow);

            if (t == null) throw new UnauthorizedAccessException();

            var media = await _db.MediaFiles.FindAsync(t.MediaId);
            if (media == null) throw new FileNotFoundException();

            var path = Path.Combine(_storageRoot, media.RelativePath);
            if (!File.Exists(path)) throw new FileNotFoundException();

            t.IsUsed = true;
            await _db.SaveChangesAsync();

            return (File.ReadAllBytes(path), media.ContentType, media.OriginalName);
        }

        public async Task<bool> DeleteFileAsync(Guid mediaId, int requestingUserId)
        {
            var media = await _db.MediaFiles.FindAsync(mediaId);
            if (media == null) return false;

            // Проверяем, что пользователь владелец
            if (media.UploadedBy != requestingUserId)
                throw new UnauthorizedAccessException("You are not allowed to delete this file.");

            var path = Path.Combine(_storageRoot, media.RelativePath);
            if (File.Exists(path))
                File.Delete(path);

            _db.MediaFiles.Remove(media);
            await _db.SaveChangesAsync();

            return true;
        }
        public async Task<bool> DeleteFileByUrlAsync(string fileUrl, int requestingUserId)
        {
            var media = _db.MediaFiles.FirstOrDefault(x => x.Url == fileUrl);
            if (media == null) return false;

            if (media.UploadedBy != requestingUserId)
                throw new UnauthorizedAccessException("You are not allowed to delete this file.");

            var path = Path.Combine(_storageRoot, media.RelativePath);
            if (File.Exists(path))
                File.Delete(path);

            _db.MediaFiles.Remove(media);
            await _db.SaveChangesAsync();

            return true;
        }

public IEnumerable<MediaFile> GetFilesByUser(int userId)
{
    return _db.MediaFiles
              .Where(f => f.UploadedBy == userId && !f.IsPrivate) // только публичные
              .OrderByDescending(f => f.UploadedAt)
              .ToList();
}
    }
}

