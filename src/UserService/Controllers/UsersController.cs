using AutoMapper;
using Loft.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using UserService.DTOs;
using UserService.Entities;
using UserService.Services;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, IWebHostEnvironment env, IMapper mapper)
        {
            _userService = userService;
            _env = env;
            _mapper = mapper;
        }

        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserById(long id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
                return NotFound(new { message = $"User with id {id} not found" });

            // AutoMapper сделает конверсию сам:
            var publicUser = _mapper.Map<PublicUserDTO>(user);

            return Ok(publicUser);
        }

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

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var existing = await _userService.GetUserById(userId.Value);
            if (existing == null) return NotFound();

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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
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

        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            await _userService.DeleteUser(userId.Value);
            return NoContent();
        }

        [HttpPost("me/toggle-seller")]
        [Authorize]
        public async Task<IActionResult> ToggleSellerStatus()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var result = await _userService.ToggleSellerStatus(userId.Value);
            if (result == null) return NotFound();

            return Ok(new
            {
                message = result.CanSell ? "You can now sell products" : "Seller status disabled",
                canSell = result.CanSell,
                user = result
            });
        }

        [HttpGet("{id}/can-sell")]
        public async Task<IActionResult> CanUserSell(long id)
        {
            var canSell = await _userService.CanUserSell(id);
            return Ok(new { userId = id, canSell });
        }

        protected long? GetUserIdFromClaims()
        {
            // Попробуем несколько общих типов соответственно возможным картам claim-ов:
            var tryTypes = new[]
            {
            ClaimTypes.NameIdentifier, // "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            "nameid",                  // иногда сокращённое имя
            JwtRegisteredClaimNames.Sub, // "sub"
            "id",
            "user_id",
            ClaimTypes.Name,
            ClaimTypes.Email
        };

            foreach (var t in tryTypes)
            {
                var claim = User.FindFirst(t)?.Value;
                if (!string.IsNullOrEmpty(claim) && long.TryParse(claim, out var id)) return id;
            }

            // Резервный метод: найти любой числовой claim в токене
            foreach (var c in User.Claims)
            {
                if (!string.IsNullOrEmpty(c.Value) && long.TryParse(c.Value, out var id)) return id;
            }

            return null;
        }
    }
 }