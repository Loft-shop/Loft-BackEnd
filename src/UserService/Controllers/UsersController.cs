using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services;
using System.Security.Claims;
using UserService.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Threading.Tasks;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;

        public UsersController(IUserService userService, IWebHostEnvironment env)
        {
            _userService = userService;
            _env = env;
        }

        // GET api/users/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var user = await _userService.GetUserById(userId.Value);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // PUT api/users/me
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var existing = await _userService.GetUserById(userId.Value);
            if (existing == null) return NotFound();

            // create new DTO using 'with' to avoid modifying init-only properties
            var toUpdate = existing with
            {
                FirstName = request.FirstName ?? existing.FirstName,
                LastName = request.LastName ?? existing.LastName,
                Phone = request.Phone ?? existing.Phone
            };

            var updated = await _userService.UpdateUser(userId.Value, toUpdate);
            return Ok(updated);
        }

        // POST api/users/me/avatar
        [HttpPost("me/avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0) return BadRequest(new { message = "No file uploaded" });
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var existing = await _userService.GetUserById(userId.Value);
            if (existing == null) return NotFound();

            // validate content type (should be image) and size (<=5MB)
            const long maxBytes = 5 * 1024 * 1024; // 5 MB
            if (!string.IsNullOrEmpty(avatar.ContentType) && !avatar.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Only image files are allowed" });
            }
            if (avatar.Length > maxBytes)
            {
                return BadRequest(new { message = "File is too large. Max 5 MB allowed" });
            }

            // ensure wwwroot/avatars exists
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var avatarsDir = Path.Combine(webRoot, "avatars");
            if (!Directory.Exists(avatarsDir)) Directory.CreateDirectory(avatarsDir);

            var fileExt = Path.GetExtension(avatar.FileName);
            var fileName = $"avatar_{userId.Value}_{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(avatarsDir, fileName);

            try
            {
                using (var stream = System.IO.File.Create(filePath))
                {
                    await avatar.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                // log if logger available; return 500 to frontend
                return StatusCode(500, new { message = "Failed to save file", detail = ex.Message });
            }

            // delete old avatar file if exists and is local
            try
            {
                if (!string.IsNullOrEmpty(existing.AvatarUrl))
                {
                    // AvatarUrl is stored as relative path like "/avatars/xyz.png"
                    var relative = existing.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var oldPath = Path.Combine(webRoot, relative);
                    if (System.IO.File.Exists(oldPath))
                    {
                        try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
                    }
                }
            }
            catch { /* ignore */ }

            // use relative path for AvatarUrl
            var relativeUrl = $"/avatars/{fileName}";

            var toUpdate = existing with { AvatarUrl = relativeUrl };
            var updated = await _userService.UpdateUser(userId.Value, toUpdate);

            return Ok(new { avatarUrl = relativeUrl, user = updated });
        }

        // DELETE api/users/me
        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            await _userService.DeleteUser(userId.Value);
            return NoContent();
        }

        private long? GetUserIdFromClaims()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim)) return null;
            if (long.TryParse(idClaim, out var id)) return id;
            return null;
        }
    }
}