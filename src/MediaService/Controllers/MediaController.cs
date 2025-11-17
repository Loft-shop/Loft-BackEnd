using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Loft.Common.DTOs;
using MediaService.Services;

namespace MediaService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly ILogger<MediaController> _logger;

        public MediaController(IMediaService mediaService, ILogger<MediaController> logger)
        {
            _mediaService = mediaService;
            _logger = logger;
        }

        /// <summary>
        /// Загрузить файл (изображение)
        /// </summary>
        /// <param name="file">Файл для загрузки</param>
        /// <param name="category">Категория (например: avatars, products, general)</param>
        /// <returns>Информация о загруженном файле</returns>
        [HttpPost("upload")]
        [Authorize]
        public async Task<ActionResult<UploadResponseDTO>> UploadFile(
            IFormFile file, 
            [FromQuery] string category = "general")
        {
            try
            {
                var result = await _mediaService.UploadFileAsync(file, category);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { error = "Internal server error while uploading file" });
            }
        }

        /// <summary>
        /// Загрузить аватар пользователя
        /// </summary>
        [HttpPost("upload/avatar")]
        [Authorize]
        public async Task<ActionResult<UploadResponseDTO>> UploadAvatar(IFormFile file)
        {
            return await UploadFile(file, "avatars");
        }

        /// <summary>
        /// Загрузить изображение продукта
        /// </summary>
        [HttpPost("upload/product")]
        [Authorize]
        public async Task<ActionResult<UploadResponseDTO>> UploadProductImage(IFormFile file)
        {
            return await UploadFile(file, "products");
        }

        /// <summary>
        /// Скачать файл по имени
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        [HttpGet("download/{fileName}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                var (stream, contentType, name) = await _mediaService.DownloadFileAsync(fileName);
                
                if (stream == null)
                {
                    return NotFound(new { error = "File not found" });
                }

                return File(stream, contentType!, name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file: {fileName}");
                return StatusCode(500, new { error = "Internal server error while downloading file" });
            }
        }

        /// <summary>
        /// Получить файл для отображения (inline)
        /// </summary>
        [HttpGet("view/{fileName}")]
        [AllowAnonymous]
        public async Task<IActionResult> ViewFile(string fileName)
        {
            try
            {
                var (stream, contentType, _) = await _mediaService.DownloadFileAsync(fileName);
                
                if (stream == null)
                {
                    return NotFound(new { error = "File not found" });
                }

                return File(stream, contentType!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing file: {fileName}");
                return StatusCode(500, new { error = "Internal server error while viewing file" });
            }
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        [HttpDelete("delete/{fileName}")]
        [Authorize]
        public async Task<ActionResult<DeleteResponseDTO>> DeleteFile(string fileName)
        {
            try
            {
                var result = await _mediaService.DeleteFileAsync(fileName);
                
                if (!result)
                {
                    return NotFound(new DeleteResponseDTO 
                    { 
                        Success = false, 
                        Message = "File not found" 
                    });
                }

                return Ok(new DeleteResponseDTO 
                { 
                    Success = true, 
                    Message = "File deleted successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {fileName}");
                return StatusCode(500, new DeleteResponseDTO 
                { 
                    Success = false, 
                    Message = "Internal server error while deleting file" 
                });
            }
        }

        /// <summary>
        /// Получить список всех файлов
        /// </summary>
        /// <param name="category">Фильтр по категории (необязательно)</param>
        [HttpGet("files")]
        [Authorize]
        public async Task<ActionResult<List<MediaFileDTO>>> GetAllFiles([FromQuery] string? category = null)
        {
            try
            {
                var files = await _mediaService.GetAllFilesAsync(category);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files list");
                return StatusCode(500, new { error = "Internal server error while getting files list" });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", service = "MediaService" });
        }
    }
}

